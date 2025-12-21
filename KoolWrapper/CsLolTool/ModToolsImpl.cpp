#include "pch.h"
#include "ModToolsImpl.h"


hash::Dict ModToolsImpl::m_hashDict{};

static bool FILTER_NONE(wad::Index::Map::const_reference i) noexcept { return false; }

static bool FILTER_TFT(wad::Index::Map::const_reference i) noexcept
{
    static constexpr std::string_view BLOCKLIST[] = {"map21", "map22"};
    for (const auto& name : BLOCKLIST)
        if (name == i.first) return true;
    return false;
}

void ModToolsImpl::InitHashDict(const fs::path& hashdictPath)
{
    if (m_hashLoaded) return;
    logi("Loading hashdict: ", hashdictPath);
    if (!m_hashDict.load(hashdictPath))
        throw std::runtime_error("Failed to load hashdict.");
    m_hashLoaded = true;
}

void ModToolsImpl::wad_exctract(const fs::path& src, fs::path dst)
{
    try
    {
        if (dst.empty())
        {
            dst = src;
            if (dst.extension().empty())
                dst.replace_extension(".wad");
            else
                dst.replace_extension();
        }

        const auto archive = wad::Archive::read_from_file(src);

        fs::create_directories(dst);

        for (const auto& entry : archive.entries)
        {
            const auto& name = entry.first;
            const auto& data = entry.second;
            data.write_to_dir(name, dst, &m_hashDict); // используем кэш
        }
    }
    catch (std::exception& ex)
    {
        loge("Extraction failed", ex.what());
        loge("src: ", src,"\n dst: ", dst);
    }
}

void ModToolsImpl::wad_pack(const fs::path& src, fs::path dst)
{
    try
    {
        if (dst.empty())
        {
            if (const auto filename = src.generic_string(); filename.ends_with(".wad"))
            {
                dst = filename + ".client";
            }
            else
            {
                dst = filename + ".wad.client";
            }
        }
        auto archive = wad::Archive{};
        for (const auto& entry : fs::recursive_directory_iterator(src))
        {
            if (!entry.is_regular_file()) continue;
            auto path = entry.path();
            archive.add_from_directory(path, src);
        }
        archive.write_to_file(dst);
    }
    catch (std::exception& ex)
    {
        loge("Pack failed", ex.what());
        loge("src: ", src,"\n dst: ", dst);
    }
}

bool ModToolsImpl::is_wad(const fs::path& path)
{
    const auto filename = path.filename().generic_string();
    if (filename.ends_with(".wad") || filename.ends_with(".wad.client")) return true;
    if (fs::exists(path / "data")) return true;
    if (fs::exists(path / "data2")) return true;
    if (fs::exists(path / "levels")) return true;
    if (fs::exists(path / "assets")) return true;
    if (fs::exists(path / "assets")) return true;
    if (fs::exists(path / "OBSIDIAN_PACKED_MAPPING.txt")) return true;
    return false;
}

void ModToolsImpl::mod_import(fs::path src, fs::path dst, fs::path game, bool noTFT)
{
    lol_trace_func(lol_trace_var("{}", src), lol_trace_var("{}", dst), lol_trace_var("{}", game));
    lol_throw_if(src.empty());
    lol_throw_if(dst.empty());

    logi("Creating tmp directory");
    dst = dst.lexically_normal();

    fs::path final_dir_name = dst.filename();

    fs::path tmp_name_suffix = final_dir_name.generic_string() + ".tmp";

    fs::path parent_dir = dst.parent_path();

    fs::path temp_dir_path = parent_dir / tmp_name_suffix;
    try
    {
        auto tmp = fs::tmp_dir{temp_dir_path};
        if (is_wad(src))
        {
            auto info_json = fmt::format(
                R"({{ "Author": "Unknown", "Description": "Imported from wad", "Name": "{}", "Version": "1.0.0" }})",
                wad::Mounted::make_name(src));
            auto info = io::File::create(tmp.path / "META" / "info.json");
            info.write(0, info_json.data(), info_json.size());
            mod_addwad(src, tmp.path, game, noTFT, false);
        }
        else if (auto filename = src.filename().generic_string();
            filename.ends_with(".zip") || filename.ends_with(".fantome"))
        {
            logi("Unzipping mod");
            utility::unzip(src, tmp.path);

            logi("Optimizing after unzipping");
            mod_copy(tmp.path, tmp.path, game, noTFT);
        }
        else if (fs::exists(src / "META" / "info.json"))
        {
            mod_copy(src, tmp.path, game, noTFT);
        }
        else
        {
            lol_throw_msg("Unsuported mod file!");
        }

        if (fs::exists(dst))
        {
            logi("Remove existing mod");
            fs::remove_all(dst);
        }

        logi("Moving tmp directory");
        tmp.move(dst);
    }
    catch (std::exception& ex)
    {
        loge("{}", ex.what());
    }
    fmtlog::flushOn(fmtlog::DBG);
}

void ModToolsImpl::mod_mkoverlay(fs::path src, fs::path dst, fs::path game, fs::names mods, bool noTFT,
                                 bool ignoreConflict)
{
    lol_trace_func(lol_trace_var("{}", src), lol_trace_var("{}", dst), lol_trace_var("{}", game));
    lol_throw_if(src.empty());
    lol_throw_if(dst.empty());
    lol_throw_if(game.empty());

    logi("Indexing game");
    auto game_index = wad::Index::from_game_folder(game);
    lol_throw_if_msg(game_index.mounts.empty(), "Not a valid Game folder");
    game_index.remove_filter(noTFT ? FILTER_TFT : FILTER_NONE);

    auto blocked = std::unordered_set<hash::Xxh64>{};
    for (const auto& [_, mounted] : game_index.mounts)
    {
        auto subchunk_name = fs::path(mounted.relpath).replace_extension(".SubChunkTOC").generic_string();
        blocked.insert(hash::Xxh64(subchunk_name));
    }
    auto mod_queue = std::vector<wad::Index>{};

    logi("Reading mods");
    for (const auto& mod_name : mods)
    {
        auto mod_index = wad::Index::from_mod_folder(src / mod_name);
        for (auto& [path_, mounted] : mod_index.mounts)
        {
            std::erase_if(mounted.archive.entries, [&](const auto& kvp) { return blocked.contains(kvp.first); });
        }
        if (mod_index.mounts.empty())
        {
            logw("Empty mod: {}", mod_index.name);
            continue;
        }

        // We have to resolve any conflicts inside mod itself
        mod_index.resolve_conflicts(mod_index, ignoreConflict);

        // We try to resolve conflicts with other mods
        for (auto& old : mod_queue)
        {
            old.resolve_conflicts(mod_index, ignoreConflict);
        }
        mod_queue.push_back(std::move(mod_index));
    }

    auto overlay_index = wad::Index{};
    logi("Merging mods");
    for (auto& mod_index : mod_queue)
    {
        overlay_index.add_overlay_mod(game_index, mod_index);
    }

    logi("Writing wads");
    fs::create_directories(dst);
    overlay_index.write_to_directory(dst);

    logi("Cleaning up stray wads");
    overlay_index.cleanup_in_directory(dst);
}

void ModToolsImpl::mod_addwad(fs::path src, fs::path dst, fs::path game, const bool noTFT, const bool removeUNK)
{
    lol_trace_func(lol_trace_var("{}", src), lol_trace_var("{}", dst), lol_trace_var("{}", game));
    lol_throw_if(src.empty());
    lol_throw_if(dst.empty());
    lol_throw_if(!fs::exists(dst / "META" / "info.json"));

    auto mounted = wad::Mounted{src.filename()};
    if (fs::is_directory(src))
    {
        mounted.archive = wad::Archive::pack_from_directory(src);
    }
    else
    {
        mounted.archive = wad::Archive::read_from_file(src);
    }

    if (!game.empty())
    {
        logi("Indexing game wads");
        auto game_index = wad::Index::from_game_folder(game);
        lol_throw_if_msg(game_index.mounts.empty(), "Not a valid Game folder");
        game_index.remove_filter(noTFT ? FILTER_TFT : FILTER_NONE);

        logi("Rebasing");
        const auto base = game_index.find_by_mount_name_or_overlap(mounted.name(), mounted.archive);
        lol_throw_if_msg(!base, "Failed to find base wad for: {}", mounted.name());
        if (removeUNK)
        {
            mounted.remove_unknown(*base);
        }
        mounted.remove_unmodified(*base);
        mounted.relpath = mounted.relpath.parent_path() / base->relpath.filename();
    }
    else
    {
        logw("No game folder selected, falling back to manual rename!");
        auto filename = mounted.relpath.filename().generic_string();
        if (!filename.ends_with(".wad.client"))
        {
            if (filename.ends_with(".wad"))
            {
                filename.append(".client");
            }
            else
            {
                filename.append(".wad.client");
            }
        }
    }

    mounted.archive.write_to_file(dst / "WAD" / mounted.relpath);
}

void ModToolsImpl::mod_copy(fs::path src, fs::path dst, fs::path game, const bool noTFT)
{
    lol_trace_func(lol_trace_var("{}", src), lol_trace_var("{}", dst), lol_trace_var("{}", game));
    lol_throw_if(src.empty());
    lol_throw_if(dst.empty());
    lol_throw_if(!fs::exists(src / "META" / "info.json"));

    if (src == dst)
    {
        logi("Creating tmp directory");
        auto tmp = fs::tmp_dir{dst.generic_string() + ".tmp"};
        mod_copy(src, tmp.path, game, noTFT);

        logi("Removing original directory");
        fs::remove_all(src);

        logi("Moving tmp directory");
        tmp.move(src);
        return;
    }

    logi("Indexing mod wads");
    auto mod_index = wad::Index::from_mod_folder(src);

    if (!game.empty())
    {
        logi("Indexing game wads");
        auto game_index = wad::Index::from_game_folder(game);
        lol_throw_if_msg(game_index.mounts.empty(), "Not a valid Game folder");
        game_index.remove_filter(noTFT ? FILTER_TFT : FILTER_NONE);

        logi("Rebasing wads");
        mod_index = mod_index.rebase_from_game(game_index);
    }

    logi("Resolving conflicts");
    mod_index.resolve_conflicts(mod_index, false);

    if (fs::exists(dst))
    {
        fs::remove_all(dst);
    }

    logi("Copying META files");
    fs::create_directories(dst / "META");
    for (const auto& dirent : fs::directory_iterator(src / "META"))
    {
        auto relpath = fs::relative(dirent.path(), src);
        fs::copy_file(src / relpath, dst / relpath, fs::copy_options::overwrite_existing);
    }

    logi("Writing wads");
    fs::create_directories(dst);
    mod_index.write_to_directory(dst);
}

void ModToolsImpl::mod_runoverlay(const fs::path& overlay, const fs::path& config_file, const fs::path& game,
                                  const fs::names& opts, std::shared_ptr<std::atomic_bool> cancelToken)
{
    auto old_msg = patcher::M_DONE;

    try
    {
        patcher::run(
            [&old_msg, cancelToken](auto msg, const char* arg)
            {
                if (cancelToken && *cancelToken)
                {
                    logi("Patcher aborted due to cancellation token");
                    throw patcher::PatcherAborted();
                }

                if (msg != old_msg)
                {
                    old_msg = msg;
                    logi("{}",patcher::STATUS_MSG[msg]);
                }
            },
            overlay,
            config_file,
            game,
            opts);
    }
    catch (const patcher::PatcherAborted&)
    {
        logi("Aborted by user");
        error::stack().clear();
    }
    catch (std::exception& ex)
    {
        loge("Error: ", ex.what());
        throw;
    }
    catch (...)
    {
        throw;
    }
}
