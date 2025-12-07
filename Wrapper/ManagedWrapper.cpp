#include "ManagedWrapper.h"

#include <complex>

#include "ModToolsImpl.h"

#include <iostream>
#include <msclr/marshal_cppstd.h>
#include <fmtlog.h>

#include "RitoBinImpl.h"

using namespace System;

namespace ManagedWrapper
{
    

    bool WadExtractor::Extract(String^ wadPath, String^ outputPath, String^ hashdictPath)
    {
        try
        {
            msclr::interop::marshal_context context;
            const auto nativeWadPath = context.marshal_as<std::string>(wadPath);
            const auto nativeOutputPath = context.marshal_as<std::string>(outputPath);
            const auto nativeHashdictPath = context.marshal_as<std::string>(hashdictPath);
            wad_extract(nativeWadPath, nativeOutputPath, nativeHashdictPath);
        }
        catch (...)
        {
            throw;
        }
    }

    ModTool::ModTool()
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

    ModTool::~ModTool()
    {
        this->!ModTool();
    }

    ModTool::!ModTool()
    {
        if (m_nativeContext)
        {
            delete m_nativeContext;
            m_nativeContext = nullptr;
        }
    }

    void ModTool::Cancel()
    {
        if (m_nativeContext && m_nativeContext->cancellationToken)
        {
            *m_nativeContext->cancellationToken = true;
        }
    }

    void ModTool::Import(String^ src, String^ dst, String^ gamePath, const bool noTFT)
    {
        msclr::interop::marshal_context context;
        const auto nativeSource = context.marshal_as<std::string>(src);
        const auto nativeDestination = context.marshal_as<std::string>(dst);
        const auto nativeGamePath = context.marshal_as<std::string>(gamePath);
        mod_import(nativeSource, nativeDestination, nativeGamePath, noTFT);
    }

    void ModTool::MkOverlay(String^ src, String^ dst, String^ gamePath, String^ mods, const bool noTFT,
                            const bool IgnoreConflicts)
    {
        msclr::interop::marshal_context context;
        const auto nativeSource = context.marshal_as<std::string>(src);
        const auto nativeDestination = context.marshal_as<std::string>(dst);
        const auto nativeGamePath = context.marshal_as<std::string>(gamePath);
        const auto nativeModsPath = context.marshal_as<std::string>(mods);
        mod_mkoverlay(nativeSource, nativeDestination, nativeGamePath, nativeModsPath, noTFT, IgnoreConflicts);
    }

    void ModTool::RunOverlay(String^ overlayPath, String^ configPath, String^ gamePath, String^ opts)
    {
        msclr::interop::marshal_context context;
        const auto nativeOverlayPath = context.marshal_as<std::string>(overlayPath);
        const auto nativeConfigPath = context.marshal_as<std::string>(configPath);
        const auto nativeGamePath = context.marshal_as<std::string>(gamePath);
        const auto nativeOptsPath = context.marshal_as<std::string>(opts);

        m_nativeContext->cancellationToken = std::make_shared<std::atomic_bool>(false);

        std::cout << nativeOverlayPath << std::endl;
        std::cout << nativeConfigPath << std::endl;
        std::cout << nativeGamePath << std::endl;
        std::cout << nativeOptsPath << std::endl;
        mod_runoverlay(nativeOverlayPath, nativeConfigPath, nativeGamePath, nativeOptsPath,
                       m_nativeContext->cancellationToken);
        m_nativeContext->cancellationToken.reset();
    }

    void RitoBin::ConvertBintoJson(String^ srcPath, String^ dstPath, String^ dirHashes)
    {
        msclr::interop::marshal_context context;
        const auto nativeSrcPath = context.marshal_as<std::string>(srcPath);
        auto nativeDstPath = context.marshal_as<std::string>(dstPath);
        const auto nativeDirHashes = context.marshal_as<std::string>(dirHashes);

        auto nativeInputFormat = "bin";
        auto nativeOutputFormat = "json";


        auto args = Args();
        args.input_file = nativeSrcPath;
        args.output_format = "json";
        args.dir = nativeDirHashes;
        args.output_file = nativeDstPath;
        args.run();
    }

    void RitoBin::ConvertJsonToBin(String^ srcPath, String^ dstPath, String^ dirHashes)
    {
        msclr::interop::marshal_context context;
        const auto nativeSrcPath = context.marshal_as<std::string>(srcPath);
        const auto nativeDstPath = context.marshal_as<std::string>(dstPath);
        const auto nativeDirHashes = context.marshal_as<std::string>(dirHashes);

        auto nativeInputFormat = "json";
        auto nativeOutputFormat = "bin";


        auto args = Args();
        args.input_file = nativeSrcPath;
        args.output_format = "bin";
        args.output_file = nativeDstPath;
        args.dir = nativeDirHashes;
        args.run();
        std::cout << args.output_file;
    }
}
