#if UNITY_EDITOR
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

            // Fallback nếu chưa cấu hình scene trong Build Settings
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

        [MenuItem("Build/1. Build Android APK", false, 10)]
        public static void PerformAndroidBuild()
        {
            Debug.Log("[BuildScript] Bat dau quy trinh kiem tra va xuat file APK...");

            // Kiểm tra module Android Build Support
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                string msg = "Module 'Android Build Support' chua duoc cai dat tren phien ban Unity 6 nay!\n\n" +
                             "Cach cai dat de xuat APK:\n" +
                             "1. Mo Unity Hub tren may cua ban.\n" +
                             "2. Vao muc 'Installs' -> Tim ban Unity 6000.5.7f1.\n" +
                             "3. Bam nut '...' (hoac banh rang) -> Chon 'Add modules'.\n" +
                             "4. Tich chon 'Android Build Support' (kem Android SDK & NDK, OpenJDK) -> Bam Install.\n" +
                             "5. Sau khi cai dat xong, bam lai menu nay de xuat file APK.";

                Debug.LogError("[BuildScript] " + msg);
                EditorUtility.DisplayDialog("Android Build Support chua duoc cai", msg, "Da Hieu");
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
                EditorUtility.DisplayDialog("Loi Build", "Khong tim thay scene nao de build!", "OK");
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

            Debug.Log($"[BuildScript] Dang xuat APK tai {AndroidApkPath} voi {scenes.Length} scene(s)...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] XUAT APK THANH CONG! Dung luong: {summary.totalSize / (1024 * 1024):F2} MB tai: {Path.GetFullPath(AndroidApkPath)}");
                EditorUtility.DisplayDialog("Xuat APK Thanh Cong",
                    $"File APK da duoc tao thanh cong tai:\n{Path.GetFullPath(AndroidApkPath)}\n\nDung luong: {summary.totalSize / (1024 * 1024):F2} MB", "Mo Thu Muc");
                EditorUtility.RevealInFinder(Path.GetFullPath(AndroidApkPath));
            }
            else
            {
                Debug.LogError($"[BuildScript] Xuat APK that bai voi loi: {summary.result}");
                EditorUtility.DisplayDialog("Loi Xuat APK", $"Qua trinh xuat APK that bai. Vui long xem chi tiet trong Console Log.", "Dong");
            }
        }

        [MenuItem("Build/2. Build Windows Standalone (.exe)", false, 20)]
        public static void PerformWindowsBuild()
        {
            Debug.Log("[BuildScript] Bat dau xuat file Windows Standalone (.exe)...");

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
                EditorUtility.DisplayDialog("Loi Build", "Khong tim thay scene nao de build!", "OK");
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
                Debug.Log($"[BuildScript] XUAT FILE WINDOWS THANH CONG tai: {Path.GetFullPath(WindowsExePath)}");
                EditorUtility.DisplayDialog("Xuat Game Thanh Cong",
                    $"File chay Windows da duoc tao tai:\n{Path.GetFullPath(WindowsExePath)}", "Mo Thu Muc");
                EditorUtility.RevealInFinder(Path.GetFullPath(WindowsExePath));
            }
            else
            {
                Debug.LogError($"[BuildScript] Xuat Windows that bai: {summary.result}");
            }
        }
    }
}
#endif
