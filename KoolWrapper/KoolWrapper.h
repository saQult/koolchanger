#pragma once
#include <atomic>
#include <codecvt>
#include <locale>
#include <memory>
#include <string>

using namespace System;
using namespace System::Runtime::InteropServices;
namespace KoolWrapper
{
    static std::wstring to_wstring(String^ s);

    static std::string wstring_to_utf8(const std::wstring& wstr);
    
    
    struct ModToolNativeContext
    {
        std::shared_ptr<std::atomic_bool> cancellationToken;
    };

    public ref class WadExtractor sealed
    {
    public:
        static void extract(String^ wadPath, String^ outputPath, String^ hashdictPath);
        static void pack(String^ srcPath, String^ dstPath);
    };

    public ref class ModTool sealed
    {
        ModToolNativeContext* m_nativeContext;

    public:
        ModTool();
        ~ModTool();
        !ModTool();

        void Cancel();
        static void Import(String^ src, String^ dst, String^ gamePath, bool noTFT);
        static void MkOverlay(String^ src, String^ dst, String^ gamePath, String^ mods, bool noTFT, bool IgnoreConflicts);
        void RunOverlay(String^ overlayPath, String^ configPath, String^ gamePath, String^ opts);
    };


    public ref class RitoBin sealed
    {
    public:
        static void ConvertBintoJson(String^ srcPath, String^ dstPath, String^ dirHashes);
        static void ConvertJsonToBin(String^ srcPath, String^ dstPath, String^ dirHashes);
    };
}
