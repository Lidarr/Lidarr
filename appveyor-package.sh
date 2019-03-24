#! /bin/bash

artifactsFolder="./_artifacts";
artifactsFolderWindows=$artifactsFolder/windows
artifactsFolderLinux=$artifactsFolder/linux
artifactsFolderMacOS=$artifactsFolder/macos
artifactsFolderMacOSApp=$artifactsFolder/macos-app

PublishArtifacts()
{
    7z a $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.windows.zip $artifactsFolderWindows/*
    
    7z a $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.osx-app.zip $artifactsFolderMacOSApp/*
    
    7z a -ttar $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.osx.tar $artifactsFolderMacOS/*
    7z a -tgzip $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.osx.tar.gz $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.osx.tar
    rm -f $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.osx.tar
    
    7z a -ttar $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.linux.tar $artifactsFolderLinux/*
    7z a -tgzip $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.linux.tar.gz $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.linux.tar
    rm -f $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.linux.tar

    if [ "${CI_WINDOWS}" = "True" ]; then
        ./setup/inno/ISCC.exe "./setup/lidarr.iss"
        cp ./setup/output/Lidarr.*windows.exe $artifactsFolder/Lidarr.${APPVEYOR_REPO_BRANCH}.${APPVEYOR_BUILD_VERSION}.windows-installer.exe        
    fi
}

PublishSourceMaps()
{
    # Only create release on develop
    # Secure SENTRY_AUTH_TOKEN will only be decoded on branch builds, not PRs
    if [ "${CI_WINDOWS}" = "True" ] && [ "${APPVEYOR_REPO_BRANCH}" = "develop" ] && [ ! -z "${SENTRY_AUTH_TOKEN}" ]; then
        echo "Uploading source maps to sentry"
        yarn sentry-cli releases new -p lidarr -p lidarr-ui "${APPVEYOR_BUILD_VERSION}-debug"
        yarn sentry-cli releases -p lidarr-ui files "${APPVEYOR_BUILD_VERSION}-debug" upload-sourcemaps _output/UI/ --rewrite
        yarn sentry-cli releases set-commits --auto "${APPVEYOR_BUILD_VERSION}-debug"
    fi
}

PublishArtifacts
PublishSourceMaps
