#pragma once

#include <cstdlib>
#include <ritobin/bin_io.hpp>
#include <ritobin/bin_unhash.hpp>
#include <optional>
#include <filesystem>
#include <iostream>

#ifdef WIN32
#include <fcntl.h>
#include <io.h>
#endif

using ritobin::Bin;
using ritobin::BinUnhasher;
using ritobin::io::DynamicFormat;
namespace fss = std::filesystem;

class Args {
public:
    bool keep_hashed = false;
    bool recursive = false;
    bool log = false;

    std::string dir;
    std::string input_file;
    std::string output_file;
    std::string input_dir;
    std::string output_dir;
    std::string input_format;
    std::string output_format;

    Args();

    template<char M>
    FILE* open_file(const std::string& name);

    void read(Bin& bin);
    void unhash(Bin& bin);
    void write(Bin& bin);
    void run_once();
    void run();

private:
    static std::optional<BinUnhasher> g_unhasher;
    static void set_binary_mode(FILE* file);
    static const DynamicFormat* get_format(const std::string& name, std::string_view data, const std::string& file_name);
};
