#include "pch.h"
#include "KoolWrapper.h"
#include "CsLolTool/ModToolsImpl.h"
#include "RitoBin/RitoBinImpl.h"


std::wstring KoolWrapper::to_wstring(String^ s)
{
    if (s == nullptr) return L"";

    IntPtr ptr = Marshal::StringToHGlobalUni(s);
    std::wstring wstr(static_cast<wchar_t*>(ptr.ToPointer()));
    Marshal::FreeHGlobal(ptr);

    return wstr;
}

std::string KoolWrapper::wstring_to_utf8(const std::wstring& wstr)
{
    std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> conv;
    return conv.to_bytes(wstr);
}

void KoolWrapper::WadExtractor::extract(String^ wadPath, String^ outputPath, String^ hashdictPath)
{
    const auto nativeWadPath = to_wstring(wadPath);
    const auto nativeOutputPath = to_wstring(outputPath);
    const auto nativeHashdictPath = to_wstring(hashdictPath);
    ModToolsImpl::InitHashDict(nativeHashdictPath);
    ModToolsImpl::wad_exctract(nativeWadPath, nativeOutputPath);
}

void KoolWrapper::WadExtractor::pack(String^ srcPath, String^ dstPath)
{
    const auto nativeSrcPath = to_wstring(srcPath);
    const auto nativeDstPath = to_wstring(dstPath);
    ModToolsImpl::wad_pack(nativeSrcPath, nativeDstPath);
}

KoolWrapper::ModTool::ModTool()
{
    m_nativeContext = new ModToolNativeContext();
    m_nativeContext->cancellationToken = nullptr;

    static FILE* g_log_file = nullptr;
    if (g_log_file == nullptr)
    {
        g_log_file = _fsopen("native_lib_direct.log", "w", _SH_DENYNO);
        if (g_log_file != nullptr)
        {
            fmtlog::setLogFile(g_log_file, false);
            setvbuf(g_log_file, nullptr, _IONBF, 0);
            fmtlog::flushOn(fmtlog::DBG);
            fmtlog::startPollingThread();
        }
    }
}

KoolWrapper::ModTool::~ModTool()
{
    this->!ModTool();
}

void KoolWrapper::ModTool::!ModTool()
{
    if (m_nativeContext)
    {
        delete m_nativeContext;
        m_nativeContext = nullptr;
    }
}

void KoolWrapper::ModTool::Cancel()
{
    if (m_nativeContext && m_nativeContext->cancellationToken)
    {
        *m_nativeContext->cancellationToken = true;
    }
}

void KoolWrapper::ModTool::Import(String^ src, String^ dst, String^ gamePath, const bool noTFT)
{
    const auto nativeSource = to_wstring(src);
    const auto nativeDestination = to_wstring(dst);
    const auto nativeGamePath = to_wstring(gamePath);
    ModToolsImpl::mod_import(nativeSource, nativeDestination, nativeGamePath, noTFT);
}

void KoolWrapper::ModTool::MkOverlay(String^ src, String^ dst, String^ gamePath, String^ mods, const bool noTFT,
                                     const bool IgnoreConflicts)
{
    const auto nativeSource = to_wstring(src);
    const auto nativeDestination = to_wstring(dst);
    const auto nativeGamePath = to_wstring(gamePath);
    const auto nativeModsPath = to_wstring(mods);
    ModToolsImpl::mod_mkoverlay(nativeSource, nativeDestination, nativeGamePath, nativeModsPath, noTFT,
                                IgnoreConflicts);
}

void KoolWrapper::ModTool::RunOverlay(String^ overlayPath, String^ configPath, String^ gamePath, String^ opts)
{
    const auto nativeOverlayPath = to_wstring(overlayPath);
    const auto nativeConfigPath = to_wstring(configPath);
    const auto nativeGamePath = to_wstring(gamePath);
    const auto nativeOptsPath = to_wstring(opts);

    m_nativeContext->cancellationToken = std::make_shared<std::atomic_bool>(false);


    ModToolsImpl::mod_runoverlay(nativeOverlayPath, nativeConfigPath, nativeGamePath, nativeOptsPath,
                                 m_nativeContext->cancellationToken);
    m_nativeContext->cancellationToken.reset();
}

void KoolWrapper::RitoBin::ConvertBintoJson(String^ srcPath, String^ dstPath, String^ dirHashes)
{
    const auto nativeSrcPath = to_wstring(srcPath);
    auto nativeDstPath = to_wstring(dstPath);
    const auto nativeDirHashes = to_wstring(dirHashes);

    auto nativeInputFormat = "bin";
    auto nativeOutputFormat = "json";


    auto args = Args();
    args.input_file = std::string(nativeSrcPath.begin(), nativeSrcPath.end());
    args.output_format = "json";
    args.dir = std::string(nativeDirHashes.begin(), nativeDirHashes.end());
    args.output_file = std::string(nativeDstPath.begin(), nativeDstPath.end());
    args.run();
}

void KoolWrapper::RitoBin::ConvertJsonToBin(String^ srcPath, String^ dstPath, String^ dirHashes)
{
    const auto nativeSrcPath = to_wstring(srcPath);
    const auto nativeDstPath = to_wstring(dstPath);
    const auto nativeDirHashes = to_wstring(dirHashes);

    auto nativeInputFormat = "json";
    auto nativeOutputFormat = "bin";


    auto args = Args();
    args.input_file = std::string(nativeSrcPath.begin(), nativeSrcPath.end());
    args.output_format = "bin";
    args.output_file = std::string(nativeDstPath.begin(), nativeDstPath.end());
    args.dir = std::string(nativeDirHashes.begin(), nativeDirHashes.end());
    args.run();
}
