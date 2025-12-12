#include "pch.h"
#include "RitoBinImpl.h"

std::optional<BinUnhasher> Args::g_unhasher = std::nullopt;

#ifdef WIN32
void Args::set_binary_mode(FILE* file) {
    if (_setmode(_fileno(file), O_BINARY) == -1) {
        throw std::runtime_error("Cannot change mode to binary!");
    }
}
#else
void Args::set_binary_mode(FILE*) {}
#endif

const DynamicFormat* Args::get_format(const std::string& name, std::string_view data, const std::string& file_name) {
    if (!name.empty()) {
        auto format = DynamicFormat::get(name);
        if (!format) {
            throw std::runtime_error("Format not found: " + name);
        }
        return format;
    } else {
        auto format = DynamicFormat::guess(data, file_name);
        if (!format) {
            throw std::runtime_error("Failed to guess format for file: " + file_name);
        }
        return format;
    }
}

Args::Args() {}

template<char M>
FILE* Args::open_file(const std::string& name) {
    char mode[] = { M, 'b', '\0'};
    if (log) {
        std::cerr << "Open file for " << mode << ": " << name << std::endl;
    }
    auto file = M == 'r' ? stdin : stdout;
    if (name == "-") {
        set_binary_mode(file);
    } else {
        if constexpr (M == 'w') {
            auto parent_dir = fss::path(name).parent_path();
            if (!parent_dir.empty()) {
                if (std::error_code ec = {}; (fss::create_directories(parent_dir, ec)), ec != std::error_code{}) {
                    throw std::runtime_error("Failed to create parent directory: " + ec.message());
                }
            }
        }
        file = fopen(name.c_str(), mode);
    }
    if (!file) {
        throw std::runtime_error("Failed to open file with mode!");
    }
    return file;
}

void Args::read(Bin& bin) {
    auto file = open_file<'r'>(input_file);

    std::vector<char> data;
    char buffer[4096];
    if (log) {
        std::cerr << "Reading..." << std::endl;
    }
    while (auto read = fread(buffer, 1, sizeof(buffer), file)) {
        data.insert(data.end(), buffer, buffer + read);
    }
    fclose(file);

    if (log) {
        std::cerr << "Parsing..." << std::endl;
    }
    auto format = get_format(input_format, std::string_view{data.data(), data.size()}, input_file);
    auto error = format->read(bin, data);
    if (!error.empty()) {
        throw std::runtime_error(error);
    }
    if (output_file.empty() && output_format.empty()) {
        output_format = format->oposite_name();
    }
}

void Args::unhash(Bin& bin) {
    if (!keep_hashed) {
        if (!g_unhasher.has_value()) {
            if (log) {
                std::cerr << "Loading hashes (Initial Load)..." << std::endl;
            }

            auto& uh = g_unhasher.emplace();

            if (dir.empty()) {
                dir = ".";
            }
            uh.load_fnv1a_CDTB(dir + "/hashes.binentries.txt");
            uh.load_fnv1a_CDTB(dir + "/hashes.binhashes.txt");
            uh.load_fnv1a_CDTB(dir + "/hashes.bintypes.txt");
            uh.load_fnv1a_CDTB(dir + "/hashes.binfields.txt");
            uh.load_xxh64_CDTB(dir + "/hashes.game.txt");
            uh.load_xxh64_CDTB(dir + "/hashes.lcu.txt");

            if (log) {
                std::cerr << "Hashes loaded successfully." << std::endl;
            }
        } else {
            if (log) {
                std::cerr << "Using cached hashes." << std::endl;
            }
        }

        if (log) {
            std::cerr << "Unhashing..." << std::endl;
        }
        g_unhasher->unhash_bin(bin, MAXINT);
    }
}

void Args::write(Bin& bin) {
    auto format = get_format(output_format, "", output_file);

    if (!keep_hashed) {
        unhash(bin);
    }
    if (output_file.empty()) {
        if (input_file == "-") {
            output_file = "-";
        } else {
            output_file = fss::path(input_file).replace_extension(format->default_extension()).generic_string();
            if (recursive && !output_dir.empty()) {
                output_file = (output_dir / fss::relative(output_file, input_dir)).generic_string();
            }
        }
    }

    if (log) {
        std::cerr << "Serializing..." << std::endl;
    }
    std::vector<char> data;
    auto error = format->write(bin, data);
    if (!error.empty()) {
        throw std::runtime_error(error);
    }

    auto file = open_file<'w'>(output_file);
    if (log) {
        std::cerr << "Writing data..." << std::endl;
    }
    fwrite(data.data(), 1, data.size(), file);
    fflush(file);
    fclose(file);
}

void Args::run_once() {
    try {
        auto bin = Bin{};
        read(bin);
        write(bin);
    } catch (const std::runtime_error& err) {
        std::cerr << "In: " << input_file << std::endl;
        std::cerr << "Out: " << output_file << std::endl;
        std::cerr << "Error: " << err.what() << std::endl;
    }
}

void Args::run() {
    if (!recursive) {
        return run_once();
    }

    if (!fss::exists(input_dir) || !fss::is_directory(input_dir)) {
        throw std::runtime_error("Input directory doesn't exist!");
    }

    if (input_format.empty()) {
        throw std::runtime_error("Recursive run needs input format!");
    }

    auto const format = get_format(input_format, "", "");
    if (!format) {
        throw std::runtime_error("No format found for recursive run!");
    }

    auto const extension = format->default_extension();
    if (extension.empty()) {
        throw std::runtime_error("Format must have default extension!");
    }

    for (auto const& entry : fss::recursive_directory_iterator(input_dir)) {
        if (!entry.is_regular_file()) {
            continue;
        }
        auto const path = entry.path();
        if (path.extension() != extension) {
            continue;
        }
        this->input_file = path.generic_string();
        Args{*this}.run_once();
    }
}
