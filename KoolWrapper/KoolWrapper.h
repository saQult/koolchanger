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
    static std::wstring to_wstring(String^ s)
    {
        if (s == nullptr) return L"";

        IntPtr ptr = Marshal::StringToHGlobalUni(s);
        std::wstring wstr(static_cast<wchar_t*>(ptr.ToPointer()));
        Marshal::FreeHGlobal(ptr);

        return wstr;
    };

    static std::string wstring_to_utf8(const std::wstring& wstr)
    {
        std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> conv;
        return conv.to_bytes(wstr);
    };
    
    
    struct ModToolNativeContext
    {
        std::shared_ptr<std::atomic_bool> cancellationToken;
    };

    public ref class WadExtractor sealed
    {
    public:
        void extract(String^ wadPath, String^ outputPath, String^ hashdictPath);
    };

    public ref class ModTool sealed
    {
        ModToolNativeContext* m_nativeContext;

    public:
        ModTool();
        ~ModTool();
        !ModTool();

        void Cancel();
        void Import(String^ src, String^ dst, String^ gamePath, bool noTFT);
        void MkOverlay(String^ src, String^ dst, String^ gamePath, String^ mods, bool noTFT, bool IgnoreConflicts);
        void RunOverlay(String^ overlayPath, String^ configPath, String^ gamePath, String^ opts);
    };


    public ref class RitoBin sealed
    {
    public:
        void ConvertBintoJson(String^ srcPath, String^ dstPath, String^ dirHashes);
        void ConvertJsonToBin(String^ srcPath, String^ dstPath, String^ dirHashes);
    };
}
