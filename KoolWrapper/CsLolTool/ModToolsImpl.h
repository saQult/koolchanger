#pragma once
#include <cstdio>
#include <lol/error.hpp>
#include <lol/fs.hpp>
#include <lol/hash/dict.hpp>
#include <lol/io/file.hpp>
#include <lol/log.hpp>
#include <lol/patcher/patcher.hpp>
#include <lol/utility/cli.hpp>
#include <lol/utility/zip.hpp>
#include <lol/wad/archive.hpp>
#include <lol/wad/index.hpp>
#include <thread>
#include <unordered_set>
#include <iostream>
#include <mutex>
using namespace lol;

class ModToolsImpl
{
public:
    static void InitHashDict(const fs::path& hashdictPath);    static bool is_wad(const fs::path& path);
    static void wad_exctract(const fs::path& src, fs::path dst);
    static void wad_pack(const fs::path& src, fs::path dst);
    static void mod_import(fs::path src, fs::path dst, fs::path game, bool noTFT);
    static void mod_mkoverlay(fs::path src, fs::path dst, fs::path game, fs::names mods, bool noTFT,bool ignoreConflict);
    static void mod_addwad(fs::path src, fs::path dst, fs::path game, bool noTFT, bool removeUNK);
    static void mod_copy(fs::path src, fs::path dst, fs::path game, bool noTFT);
    static void mod_runoverlay(const fs::path& overlay, const fs::path& config_file, const fs::path& game,
        const fs::names& opts, std::shared_ptr<std::atomic_bool> cancelToken);
private:
    static hash::Dict m_hashDict;
    inline static bool m_hashLoaded = false;
};
