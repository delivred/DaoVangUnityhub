#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DaoVang.Editor
{
    public static class BuildScript
    {
        private const string AndroidPackageName = "com.daovang.goldminer";
        private const string AppProductName = "Dao Vang";
        private const string AndroidApkPath = "Builds/Android/DaoVang.apk";
        private const string WindowsExePath = "Builds/Windows/DaoVang.exe";

        private static string[] GetScenesToBuild()
        {
            var activeScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();

            if (activeScenes.Length > 0)
            {
                return activeScenes;
            }

            string defaultScene = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(defaultScene))
            {
                return new[] { defaultScene };
            }

            string altScene = "Assets/hierarchy.unity";
            if (File.Exists(altScene))
            {
                return new[] { altScene };
            }

            return new string[0];
        }

        private static void SetupAndroidTools()
        {
            try
            {
                string editorDir = Path.GetDirectoryName(EditorApplication.applicationPath);
                string playbackEngine = Path.Combine(editorDir, "Data", "PlaybackEngines", "AndroidPlayer");
                string jdkPath = Path.Combine(playbackEngine, "OpenJDK");
                string sdkPath = Path.Combine(playbackEngine, "SDK");
                string ndkPath = Path.Combine(playbackEngine, "NDK");

                if (Directory.Exists(jdkPath))
                {
                    EditorPrefs.SetString("JdkPath", jdkPath);
                    Debug.Log($"[BuildScript] Cấu hình JdkPath: {jdkPath}");
                }
                if (Directory.Exists(sdkPath))
                {
                    EditorPrefs.SetString("AndroidSdkRoot", sdkPath);
                    Debug.Log($"[BuildScript] Cấu hình AndroidSdkRoot: {sdkPath}");
                }
                if (Directory.Exists(ndkPath))
                {
                    EditorPrefs.SetString("AndroidNdkRootR27", ndkPath);
                    EditorPrefs.SetString("AndroidNdkRootR26", ndkPath);
                    EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
                    Debug.Log($"[BuildScript] Cấu hình AndroidNdkRoot: {ndkPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildScript] Không thể cấu hình đường dẫn SDK qua EditorPrefs: {ex.Message}");
            }
        }

        [MenuItem("Build/1. Build Android APK", false, 10)]
        public static void PerformAndroidBuild()
        {
            Debug.Log("[BuildScript] Bắt đầu quy trình xuất file APK...");

            SetupAndroidTools();

            // Chuyển build target sang Android nếu chưa phải
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[BuildScript] Đang chuyển Active Build Target sang Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // Kiểm tra module Android Build Support
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                string msg = "Module 'Android Build Support' chưa được sẵn sàng trên phiên bản Unity này!";
                Debug.LogError("[BuildScript] " + msg);
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Android Build Support chưa sẵn sàng", msg, "Đã Hiểu");
                }
                return;
            }

            // Cấu hình PlayerSettings cho Android
            PlayerSettings.productName = AppProductName;
            PlayerSettings.companyName = "DefaultCompany";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidPackageName);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Đảm bảo thư mục lưu trữ tồn tại
            string dir = Path.GetDirectoryName(AndroidApkPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string[] scenes = GetScenesToBuild();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] Không tìm thấy scene nào để build!");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Lỗi Build", "Không tìm thấy scene nào để build!", "OK");
                }
                return;
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = AndroidApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildScript] Đang xuất APK tại {AndroidApkPath} với {scenes.Length} scene(s)...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] XUẤT APK THÀNH CÔNG! Dung lượng: {summary.totalSize / (1024 * 1024):F2} MB tại: {Path.GetFullPath(AndroidApkPath)}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Xuất APK Thành Công",
                        $"File APK đã được tạo thành công tại:\n{Path.GetFullPath(AndroidApkPath)}\n\nDung lượng: {summary.totalSize / (1024 * 1024):F2} MB", "Mở Thư Mục");
                    EditorUtility.RevealInFinder(Path.GetFullPath(AndroidApkPath));
                }
            }
            else
            {
                Debug.LogError($"[BuildScript] Xuất APK thất bại với lỗi: {summary.result}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Lỗi Xuất APK", $"Quá trình xuất APK thất bại. Vui lòng xem chi tiết trong Console Log.", "Đóng");
                }
            }
        }

        [MenuItem("Build/2. Build Windows Standalone (.exe)", false, 20)]
        public static void PerformWindowsBuild()
        {
            Debug.Log("[BuildScript] Bắt đầu xuất file Windows Standalone (.exe)...");

            PlayerSettings.productName = AppProductName;
            PlayerSettings.companyName = "DefaultCompany";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

            string dir = Path.GetDirectoryName(WindowsExePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string[] scenes = GetScenesToBuild();
            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] Không tìm thấy scene nào để build!");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Lỗi Build", "Không tìm thấy scene nào để build!", "OK");
                }
                return;
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WindowsExePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] XUẤT FILE WINDOWS THÀNH CÔNG tại: {Path.GetFullPath(WindowsExePath)}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Xuất Game Thành Công",
                        $"File chạy Windows đã được tạo tại:\n{Path.GetFullPath(WindowsExePath)}", "Mở Thư Mục");
                    EditorUtility.RevealInFinder(Path.GetFullPath(WindowsExePath));
                }
            }
            else
            {
                Debug.LogError($"[BuildScript] Xuất Windows thất bại: {summary.result}");
            }
        }
    }
}
#endif
