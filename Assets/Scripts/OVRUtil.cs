using UnityEngine;
using Valve.VR;
using System;
using System.Runtime.InteropServices;

namespace OVRUtil
{
    public static class System {
        public static void Init() {
            if (OpenVR.System != null) return;

            var error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Overlay);
            if (error != EVRInitError.None) {
                throw new Exception("OpenVR failed to initialize: " + error);
            }

            SteamVR_Settings.instance.pauseGameWhenDashboardVisible = false;
        }

        
        public static void Shutdown() {
            if (OpenVR.System != null) OpenVR.Shutdown();
        }
    }

    
    public static class Overlay {
        public static ulong Create(string key, string name) {
            var handle = OpenVR.k_ulOverlayHandleInvalid;
            var error = OpenVR.Overlay.CreateOverlay(key, name, ref handle);
            if (error != EVROverlayError.None) {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                UnityEngine.Application.Quit();
                #endif
            } return handle;
        }

        public static (ulong, ulong) CreateDashboard(string key, string name) {
            ulong dashboardHandle = 0;
            ulong thumbnailHandle = 0;
            var error = OpenVR.Overlay.CreateDashboardOverlay(key, name, ref dashboardHandle, ref thumbnailHandle);
            if (error != EVROverlayError.None) {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                UnityEngine.Application.Quit();
                #endif
            }
            return (dashboardHandle, thumbnailHandle);
        }

        public static void Destroy(ulong handle) {
            if (handle != OpenVR.k_ulOverlayHandleInvalid && OpenVR.Overlay != null) {
                var error = OpenVR.Overlay.DestroyOverlay(handle);
                if (error != EVROverlayError.None) {
                    throw new Exception("Failed to destroy Overlay: " + error);
                }
            }
        }

        public static void SetFromFile(ulong handle, string path) {
            var error = OpenVR.Overlay.SetOverlayFromFile(handle, path);
            if (error != EVROverlayError.None) {
                throw new Exception("Drawing obs-icon failed: " + error);
            }
        }

        public static void Show(ulong handle) {
            var error = OpenVR.Overlay.ShowOverlay(handle);
            if (error != EVROverlayError.None) {
                throw new Exception("Showing overlay failed: " + error);
            }
        }

        public static void Hide(ulong handle) {
            var error = OpenVR.Overlay.HideOverlay(handle);
            if (error != EVROverlayError.None) {
                throw new Exception("Failed to hide overlay: " + error);
            }
        }

        public static bool DashboardOverlayVisibility(ulong handle) {
            return OpenVR.Overlay.IsActiveDashboardOverlay(handle);
        }

        public static void SetSize(ulong handle, float size) {
            var error = OpenVR.Overlay.SetOverlayWidthInMeters(handle, size);
            if (error != EVROverlayError.None) {
                throw new Exception("Failed to set overlay size: " + error);
            }
        }

        public static void SetTransformAbsolute(ulong handle, Vector3 position, Quaternion rotation) {
            var rigidTransform = new SteamVR_Utils.RigidTransform(position, rotation);
            var matrix = rigidTransform.ToHmdMatrix34();
            var error = OpenVR.Overlay.SetOverlayTransformAbsolute(
                handle, ETrackingUniverseOrigin.TrackingUniverseStanding, ref matrix);
            if (error != EVROverlayError.None) {
                throw new Exception("Failed to set overlay position: " + error);
            }
        }

        public static void SetTransformRelative(ulong handle, uint deviceIndex, Vector3 position, Quaternion rotation) {
            var rigidTransform = new SteamVR_Utils.RigidTransform(position, rotation);
            var matrix = rigidTransform.ToHmdMatrix34();
            var error = OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative(handle, deviceIndex, ref matrix);
            if (error != EVROverlayError.None) {
                throw new Exception("Failed to set overlay position: " + error);
            }
        }

        public static void FlipVertical(ulong handle) {
            var bounds = new VRTextureBounds_t {
                uMin = 0,
                uMax = 1,
                vMin = 1,
                vMax = 0
            };

            var error = OpenVR.Overlay.SetOverlayTextureBounds(handle, ref bounds);
            if (error != EVROverlayError.None) {
                throw new Exception("Failed to flip texture: " + error);
            }
        }

        public static void SetRenderTexture(ulong handle, RenderTexture renderTexture) {
            if (!renderTexture.IsCreated()) return;

            var nativeTexturePtr = renderTexture.GetNativeTexturePtr();
            var texture = new Texture_t {
                eColorSpace = EColorSpace.Auto,
                eType = ETextureType.DirectX,
                handle = nativeTexturePtr
            };
            var error = OpenVR.Overlay.SetOverlayTexture(handle, ref texture);
            if (error != EVROverlayError.None) {
                throw new Exception("Failed to draw texture: " + error);
            }
        }

        public static void SetMouseScale(ulong handle, int x, int y) {
            var pvecMouseScale = new HmdVector2_t() {
                v0 = x,
                v1 = y
            };
            var error = OpenVR.Overlay.SetOverlayMouseScale(handle, ref pvecMouseScale);
            if (error != EVROverlayError.None) {
                throw new Exception("Failed to set mouse scaling factor: " + error);
            }
        }
    }
}

