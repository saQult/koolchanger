#pragma once

#include <atomic>
#include <memory>
#include <windows.h> 

using namespace System;

namespace ManagedWrapper
{
    struct ModToolNativeContext {
        std::shared_ptr<std::atomic_bool> cancellationToken;
    };
    
    public ref class WadExtractor
    {
    public:
        // Управляемый метод
        bool Extract(String^ wadPath, String^ outputPath, String^ hashdictPath);
    };
    public ref class ModTool
    {
    private:
        ModToolNativeContext* m_nativeContext;
    public:
        ModTool();
        ~ModTool(); // Dispose (IDisposable)
        !ModTool(); // Finalizer
        
        
        void Cancel();
        void Import(String^ src, String^ dst, String^ gamePath, bool noTFT);
        void MkOverlay(String^ src, String^ dst, String^ gamePath, String^ mods, bool noTFT, bool IgnoreConflicts); 
        void RunOverlay(String^ overlayPath, String^ configPath, String^ gamePath, String^ opts);
    };
    public ref class RitoBin
    {
    public:
        void ConvertBintoJson(String^ srcPath, String^ dstPath, String^ dirHashes);
        void ConvertJsonToBin(String^ srcPath, String^ dstPath, String^ dirHashes);
    // void ConverJsontoBin();
    };
    
}
