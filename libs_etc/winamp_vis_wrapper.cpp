#include "winamp_vis_wrapper.h"
#include <windows.h>
#include <iostream>

// Load a plugin DLL and return its header
winampVisHeader* LoadPlugin(const char* dllPath) {
    std::cout << "Loading plugin: " << dllPath << std::endl;
    
    // Load the DLL
    HMODULE hModule = LoadLibraryA(dllPath);
    if (!hModule) {
        std::cout << "Failed to load DLL: " << GetLastError() << std::endl;
        return nullptr;
    }
    
    // Get the winampVisGetHeader function
    typedef winampVisHeader* (*WinampVisGetHeaderFunc)();
    WinampVisGetHeaderFunc getHeader = (WinampVisGetHeaderFunc)GetProcAddress(hModule, "winampVisGetHeader");
    
    if (!getHeader) {
        std::cout << "Failed to find winampVisGetHeader function" << std::endl;
        FreeLibrary(hModule);
        return nullptr;
    }
    
    // Call the function to get the header
    winampVisHeader* header = getHeader();
    if (!header) {
        std::cout << "getHeader() returned null" << std::endl;
        FreeLibrary(hModule);
        return nullptr;
    }
    
    std::cout << "Successfully loaded plugin: " << header->description << std::endl;
    return header;
}

// Get a module from the plugin header
winampVisModule* GetModule(winampVisHeader* header, int index) {
    if (!header || !header->getModule) {
        return nullptr;
    }
    
    return header->getModule(index);
}

// Initialize a module
int InitModule(winampVisModule* module) {
    if (!module || !module->Init) {
        return 0;
    }
    
    return module->Init(module);
}

// Render a module
int RenderModule(winampVisModule* module) {
    if (!module || !module->Render) {
        return 1; // Error
    }
    
    return module->Render(module);
}

// Quit a module
void QuitModule(winampVisModule* module) {
    if (module && module->Quit) {
        module->Quit(module);
    }
}

// Configure a module
void ConfigModule(winampVisModule* module) {
    if (module && module->Config) {
        module->Config(module);
    }
}
