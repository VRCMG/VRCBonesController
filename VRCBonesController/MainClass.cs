using ActionMenuApi;
using ActionMenuApi.Types;
using Il2CppSystem.Reflection;
using LiteNetLib;
using LiteNetLib.Utils;
using MelonLoader;
using RootMotion.FinalIK;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.XR;

namespace VRCBonesController
{
    public class MainClass : MelonLoader.MelonMod
    {
        private EventBasedNetListener listener = new EventBasedNetListener();
        private NetManager manager;

        readonly Quaternion c_hmdRotationFix = new Quaternion(0f, 0.7071068f, 0.7071068f, 0f);

        Vector3 m_rootOffset = new Vector3(0f, 0.5f, 0.25f);

        static bool ms_inVrMode = false;
        static float[] m_fingersBends = null;
        static float[] m_fingersSpreads = null;

        Vector3 m_leftTargetPosition;
        Quaternion m_leftTargetRotation;
        Vector3 m_rightTargetPosition;
        Quaternion m_rightTargetRotation;

        static Vector3 rightHandPosition = new Vector3(-120f, 390f, 65.6f);
        static Vector3 leftHandPosition = new Vector3(120f, 390f, 65.6f);

        Quaternion rightHandRotation = new Quaternion(0f, 0f, 0f, 0f);
        Quaternion leftHandRotation = new Quaternion(0f, 0f, 0f, 0f);

        Quaternion cameraTransformOriginal;

        Quaternion handLeftOrginal;
        Quaternion handRightOrginal;

        static Transform cameraTransform = null;

        static bool isReady = false;
        int headMoveType = 0;

        float moveSpeed = 0.1f;
        float moveSpeedHead = 3f;

        float spreadSpeed = 0.01f;


        public bool IsManualControl = false;

        bool sync_Head = true;
        bool sync_Hands = true;
        bool sync_Fingers = true;
        bool sync_Legs = true;
        bool auto_connect = false;
        bool is_connected = false;
        bool avatar_sync = false;

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory("VRCBonesController", "VRCBonesController");
            MelonPreferences.CreateEntry<string>("VRCBonesController", "host_ip", "localhost", "IP Address which client will be connected after start vrchat.");
            MelonPreferences.CreateEntry<int>("VRCBonesController", "host_port", 7777, "Port of host.");
            MelonPreferences.CreateEntry<string>("VRCBonesController", "host_token", "3DPaED", "Token is needed to connect.");
            MelonPreferences.CreateEntry<bool>("VRCBonesController", "sync_Head", true, "Receive sync of head while client mode.");
            MelonPreferences.CreateEntry<bool>("VRCBonesController", "sync_Hands", true, "Receive sync of hands while client mode.");
            MelonPreferences.CreateEntry<bool>("VRCBonesController", "sync_Fingers", true, "Receive sync of fingers while client mode.");
            MelonPreferences.CreateEntry<bool>("VRCBonesController", "sync_legs", false, "Receive sync of legs while client mode.");
            MelonPreferences.CreateEntry<bool>("VRCBonesController", "auto_connect", true, "Auto host or auto connect if disconnected.");
            MelonPreferences.CreateEntry<bool>("VRCBonesController", "avatar_sync", false, "Sync your avatar with others ( Sync only: Hands, Head, Fingers )");

            m_fingersBends = new float[10];
            m_fingersSpreads = new float[10];

            // Patches
            var l_patchMethod = new Harmony.HarmonyMethod(typeof(MainClass), "VRCIM_ControllersType");
            typeof(VRCInputManager).GetMethods().Where(x =>
                    x.Name.StartsWith("Method_Public_Static_Boolean_EnumNPublicSealedvaKeMoCoGaViOcViDaWaUnique_")
                ).ToList().ForEach(m => Harmony.Patch(m, l_patchMethod));


            iconsAssetBundle = AssetBundle.LoadFromMemory_Internal(VRCBonesController.Properties.Resources.customicons, 0);
            iconsAssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            

            radialIcon = iconsAssetBundle.LoadAsset_Internal("Assets/Resources/Icons/sound-full.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            radialIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            toggleIcon = iconsAssetBundle.LoadAsset_Internal("Assets/Resources/Icons/zero.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            toggleIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            subMenuIcon = iconsAssetBundle.LoadAsset_Internal("Assets/Resources/Icons/file-transfer.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            subMenuIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            buttonIcon = iconsAssetBundle.LoadAsset_Internal("Assets/Resources/Icons/cloud-data-download.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
            buttonIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            try
            {
                AMAPI.AddModFolder(
                    "BonesController",
                    delegate
                    {
                        AMAPI.AddSubMenuToSubMenu("Hands", delegate ()
                        {
                            AMAPI.AddSubMenuToSubMenu("Right", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control pos", ChangeRightHandPos, radialIcon);
                                AMAPI.AddFourAxisPedalToSubMenu("Control forward/backwards", ChangeRightHandFB, radialIcon, "Forward", "", "Backwards", "");
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    rightHandPosition = new Vector3(-120f, 390f, 65.6f);
                                });
                            }, radialIcon);
                            AMAPI.AddSubMenuToSubMenu("Both", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control pos", ChangeBothHandsPos, radialIcon);
                                AMAPI.AddFourAxisPedalToSubMenu("Control forward/backwards", ChangeBothHandsFB, radialIcon, "Forward", "", "Backwards", "");
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    leftHandPosition = new Vector3(120f, 390f, 65.6f);
                                    rightHandPosition = new Vector3(-120f, 390f, 65.6f);
                                });
                            }, radialIcon);
                            AMAPI.AddSubMenuToSubMenu("Left", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control pos", ChangeLeftHandPos, radialIcon);
                                AMAPI.AddFourAxisPedalToSubMenu("Control forward/backwards", ChangeLeftHandFB, radialIcon, "Forward", "", "Backwards", "");
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    leftHandPosition = new Vector3(120f, 390f, 65.6f);
                                });
                            }, radialIcon);
                        }, radialIcon);
                        AMAPI.AddSubMenuToSubMenu("Legs", delegate ()
                        {
                            AMAPI.AddSubMenuToSubMenu("Right", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control bends", ChangeRightLegBends, radialIcon);
                                AMAPI.AddFourAxisPedalToSubMenu("Control pos", ChangeRightLegPos, radialIcon);
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    l_solver.rightLeg.IKPosition = l_solver.headPosition;
                                });
                            }, radialIcon);
                            AMAPI.AddSubMenuToSubMenu("Both", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control bends", ChangeBothLegsBends, radialIcon);
                                AMAPI.AddFourAxisPedalToSubMenu("Control pos", ChangeBothLegsPos, radialIcon);
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    l_solver.rightLeg.IKPosition = l_solver.headPosition;
                                    l_solver.leftLeg.IKPosition = l_solver.headPosition;
                                });
                            }, radialIcon);
                            AMAPI.AddSubMenuToSubMenu("Left", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control bends", ChangeLeftLegBends, radialIcon);
                                AMAPI.AddFourAxisPedalToSubMenu("Control pos", ChangeLeftLegPos, radialIcon);
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    l_solver.rightLeg.IKPosition = l_solver.headPosition;
                                });
                            }, radialIcon);
                        }, radialIcon);
                        AMAPI.AddSubMenuToSubMenu("Fingers", delegate ()
                        {
                            AMAPI.AddSubMenuToSubMenu("Right", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control", ChangeFingersRight, radialIcon);
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    ResetFingers();
                                });
                            }, radialIcon);
                            AMAPI.AddSubMenuToSubMenu("Both", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control", ChangeFingersBoth, radialIcon);
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    ResetFingers();
                                });
                            }, radialIcon);
                            AMAPI.AddSubMenuToSubMenu("Left", delegate ()
                            {
                                AMAPI.AddFourAxisPedalToSubMenu("Control", ChangeFingersLeft, radialIcon);
                                AMAPI.AddButtonPedalToSubMenu("Reset", delegate ()
                                {
                                    ResetFingers();
                                });
                            }, radialIcon);
                        }, radialIcon);
                        AMAPI.AddTogglePedalToSubMenu("Enable control", false, ChangeControlState, radialIcon);
                    //AMAPI.AddFourAxisPedalToSubMenu("Head", ChangeHeadRot, radialIcon);
                },
                    subMenuIcon
                );
            }catch(Exception)
            {
                MelonLogger.Msg("Failed to load ActionMenuAPI, not installed.");
            }

            




            OnPreferencesSaved();
        }

        private static void ChangeBothLegsPos(Vector2 pos)
        {
            if (l_solver != null)
            {
                l_solver.leftLeg.positionWeight = 1f;
                l_solver.rightLeg.positionWeight = 1f;
                l_solver.leftLeg.IKPosition = new Vector3(l_solver.leftLeg.IKPosition.x + pos.x, l_solver.leftLeg.IKPosition.y + pos.y, l_solver.leftLeg.IKPosition.z);
                l_solver.rightLeg.IKPosition = new Vector3(l_solver.rightLeg.IKPosition.x + pos.x, l_solver.rightLeg.IKPosition.y + pos.y, l_solver.rightLeg.IKPosition.z);
            }
        }

        private static void ChangeLeftLegPos(Vector2 pos)
        {
            if (l_solver != null)
            {
                l_solver.leftLeg.positionWeight = 1f;
                l_solver.leftLeg.IKPosition = new Vector3(l_solver.leftLeg.IKPosition.x + pos.x, l_solver.leftLeg.IKPosition.y + pos.y, l_solver.leftLeg.IKPosition.z);
            }
        }

        private static void ChangeRightLegPos(Vector2 pos)
        {
            if (l_solver != null)
            {
                l_solver.rightLeg.positionWeight = 1f;
                l_solver.rightLeg.IKPosition = new Vector3(l_solver.rightLeg.IKPosition.x + pos.x, l_solver.rightLeg.IKPosition.y + pos.y, l_solver.rightLeg.IKPosition.z);
            }
        }

        private static void ChangeBothLegsBends(Vector2 pos)
        {
            if (l_solver != null)
            {
                l_solver.leftLeg.rotationWeight = 1f;
                l_solver.rightLeg.rotationWeight = 1f;
                l_solver.leftLeg.IKRotation = new Quaternion(l_solver.leftLeg.IKRotation.x + pos.x, l_solver.leftLeg.IKRotation.y + pos.y, l_solver.leftLeg.IKRotation.z, l_solver.leftLeg.IKRotation.w);
                l_solver.rightLeg.IKRotation = new Quaternion(l_solver.rightLeg.IKRotation.x + pos.x, l_solver.rightLeg.IKRotation.y + pos.y, l_solver.rightLeg.IKRotation.z, l_solver.rightLeg.IKRotation.w);
            }
        }

        private static void ChangeLeftLegBends(Vector2 pos)
        {
            if (l_solver != null)
            {
                l_solver.leftLeg.rotationWeight = 1f;
                l_solver.leftLeg.IKRotation = new Quaternion(l_solver.leftLeg.IKRotation.x + pos.x, l_solver.leftLeg.IKRotation.y + pos.y, l_solver.leftLeg.IKRotation.z, l_solver.leftLeg.IKRotation.w);
            }
        }

        private static void ChangeRightLegBends(Vector2 pos)
        {
            if (l_solver != null)
            {
                l_solver.rightLeg.rotationWeight = 1f;
                l_solver.rightLeg.IKRotation = new Quaternion(l_solver.rightLeg.IKRotation.x + pos.x, l_solver.rightLeg.IKRotation.y + pos.y, l_solver.rightLeg.IKRotation.z, l_solver.rightLeg.IKRotation.w);
            }
        }


        private static void ChangeFingersLeft(Vector2 pos)
        {
            for(int x = 0; x < 5; x++)
            {
                m_fingersBends[x] = Mathf.Clamp(m_fingersBends[x] + pos.x/3, 0f, 1f);
                m_fingersSpreads[x] = Mathf.Clamp(m_fingersSpreads[x] + pos.y/3, 0f, 1f);
            }
        }


        private static void ChangeFingersRight(Vector2 pos)
        {
            for (int x = 5; x < 10; x++)
            {
                m_fingersBends[x] = Mathf.Clamp(m_fingersBends[x] + pos.x/3, 0f, 1f);
                m_fingersSpreads[x] = Mathf.Clamp(m_fingersSpreads[x] + pos.y/3, 0f, 1f);
            }
        }
        private static void ChangeFingersBoth(Vector2 pos)
        {
            for (int x = 0; x < 10; x++)
            {
                m_fingersBends[x] = Mathf.Clamp(m_fingersBends[x] + pos.x/3, 0f, 1f);
                m_fingersSpreads[x] = Mathf.Clamp(m_fingersSpreads[x] + pos.y/3, 0f, 1f);
            }
        }

        private static void ChangeControlState(bool state)
        {
            isReady = state;
            if (!isReady)
            {
                isReady = true;
                rightHandPosition = new Vector3(-120f, 390f, 65.6f);
                leftHandPosition = new Vector3(120f, 390f, 65.6f);
            }
            else
            {
                ResetFingers();
                isReady = false;
            }
        }


        private static void ChangeLeftHandPos(Vector2 pos)
        {
            leftHandPosition = new Vector3(leftHandPosition.x - pos.x, leftHandPosition.y, leftHandPosition.z - pos.y);
        }

        private static void ChangeRightHandPos(Vector2 pos)
        {
            rightHandPosition = new Vector3(rightHandPosition.x - pos.x, rightHandPosition.y, rightHandPosition.z - pos.y);
        }

        private static void ChangeBothHandsPos(Vector2 pos)
        {
            leftHandPosition = new Vector3(leftHandPosition.x - pos.x, leftHandPosition.y, leftHandPosition.z - pos.y);
            rightHandPosition = new Vector3(rightHandPosition.x - pos.x, rightHandPosition.y, rightHandPosition.z - pos.y);
        }

        private static void ChangeBothHandsFB(Vector2 pos)
        {
            leftHandPosition = new Vector3(leftHandPosition.x, leftHandPosition.y + pos.y, leftHandPosition.z);
            rightHandPosition = new Vector3(rightHandPosition.x, rightHandPosition.y + pos.y, rightHandPosition.z);
        }

        private static void ChangeLeftHandFB(Vector2 pos)
        {
            leftHandPosition = new Vector3(leftHandPosition.x, leftHandPosition.y + pos.y, leftHandPosition.z);
        }

        private static void ChangeRightHandFB(Vector2 pos)
        {
            rightHandPosition = new Vector3(leftHandPosition.x, leftHandPosition.y + pos.y, leftHandPosition.z);
        }

        private static void ChangeHeadRot(Vector2 rot)
        {
            cameraTransform.rotation = new Quaternion(
                cameraTransform.rotation.x,
                cameraTransform.rotation.y + rot.x,
                cameraTransform.rotation.z,
                cameraTransform.rotation.w);
        }

        //
        private static AssetBundle iconsAssetBundle = null;
        private static Texture2D toggleIcon;
        private static Texture2D radialIcon;
        private static Texture2D subMenuIcon;
        private static Texture2D buttonIcon;
        //


        bool isHost = true;
        string token = "";
        string host_ip = "";
        int host_port = 7777;

        public override void VRChat_OnUiManagerInit()
        {
            base.VRChat_OnUiManagerInit();
            var camera = UnityEngine.Object.FindObjectOfType<VRCVrCamera>();
            var transform = camera.GetIl2CppType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == Il2CppType.Of<Transform>()).ToArray()[0];
            cameraTransform = transform.GetValue(camera).Cast<Transform>();
            cameraTransformOriginal = cameraTransform.rotation;
        }

        public override void OnPreferencesSaved()
        {
            host_ip = MelonPreferences.GetEntryValue<string>("VRCBonesController", "host_ip");
            host_port = MelonPreferences.GetEntryValue<int>("VRCBonesController", "host_port");
            token = MelonPreferences.GetEntryValue<string>("VRCBonesController", "host_token");
            sync_Head = MelonPreferences.GetEntryValue<bool>("VRCBonesController", "sync_Head");
            sync_Hands = MelonPreferences.GetEntryValue<bool>("VRCBonesController", "sync_Hands");
            sync_Fingers = MelonPreferences.GetEntryValue<bool>("VRCBonesController", "sync_Fingers");
            sync_Legs = MelonPreferences.GetEntryValue<bool>("VRCBonesController", "sync_Legs");
            auto_connect = MelonPreferences.GetEntryValue<bool>("VRCBonesController", "auto_connect");
            avatar_sync = MelonPreferences.GetEntryValue<bool>("VRCBonesController", "avatar_sync");

          
            if (!avatar_sync)
                return;
            listener.ConnectionRequestEvent += ConnectionRequestEvent;
            listener.PeerConnectedEvent += ConnectedEvent;
            listener.PeerDisconnectedEvent += DisconnectedEvent;
            listener.NetworkReceiveEvent += NetworkReceiveEvent;
            listener.NetworkErrorEvent += NetworkErrorEvent;
            MelonLogger.Msg($" [AvatarSync] Connection token \"{token}\".");
            MelonLogger.Msg(" [AvatarSync] Current avatar sync mode \"HOST\", change via F9 key.");
            MelonLogger.Msg(" [AvatarSync] If you want to start avatar sync press F10 key.");
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(15);
                    if (manager != null)
                        manager.PollEvents();
                }
            });
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(3000);
                    if (!is_connected && !isHost)
                    {
                        if (manager == null)
                            manager = new NetManager(listener);
                        manager.Start();
                        MelonLogger.Msg($" [AvatarSync] Connecting to host....");
                        manager.Connect(host_ip, host_port, token);
                    }
                    else if (isHost && manager == null)
                    {
                        manager = new NetManager(listener);
                        manager.Start(host_port);
                    }
                }
            });
        }

        private void NetworkErrorEvent(IPEndPoint endPoint, SocketError socketError)
        {
            MelonLogger.Msg($" [AvatarSync] Network error, IP: {endPoint.Address}, Error: {socketError}");
        }

        private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte type = reader.GetByte();
            switch (type)
            {
                //hands
                case 0:
                    if (!sync_Hands)
                        return;
                    var pos = reader.GetFloatArray();
                    var rot = reader.GetFloatArray();
                    rightHandPosition = new Vector3(pos[0], pos[1], pos[2]);
                    leftHandPosition = new Vector3(pos[3], pos[4], pos[5]);
                    rightHandRotation = new Quaternion(rot[0], rot[1], rot[2], rot[3]);
                    leftHandRotation = new Quaternion(rot[4], rot[5], rot[6], rot[7]);
                    break;
                //head
                case 1:
                    if (!sync_Head)
                        return;
                    var pos2 = reader.GetFloatArray();
                    var rot2 = reader.GetFloatArray();
                    cameraTransform.rotation = new Quaternion(rot2[0], rot2[1], rot2[2], rot2[3]);
                    //cameraTransform.localPosition = new Vector3(pos2[0], pos2[1], pos2[2]);
                    break;
                //fingers
                case 2:
                    if (!sync_Fingers)
                        return;
                    m_fingersBends = reader.GetFloatArray();
                    m_fingersSpreads = reader.GetFloatArray();
                    break;
                //legs
                case 3:
                    if (!sync_Legs)
                        return;
                    if (l_solver.leftLeg != null && l_solver.rightLeg != null)
                    {
                        var pos3 = reader.GetFloatArray();
                        var rot3 = reader.GetFloatArray();
                        l_solver.leftLeg.positionWeight = 1f;
                        l_solver.leftLeg.rotationWeight = 1f;
                        l_solver.rightLeg.positionWeight = 1f;
                        l_solver.rightLeg.rotationWeight = 1f;
                        l_solver.leftLeg.IKPosition = new Vector3(pos3[0], pos3[1], pos3[2]);
                        l_solver.rightLeg.IKPosition = new Vector3(pos3[3], pos3[4], pos3[5]);
                        l_solver.leftLeg.IKRotation = new Quaternion(rot3[0], rot3[1], rot3[2], rot3[3]);
                        l_solver.rightLeg.IKRotation = new Quaternion(rot3[4], rot3[5], rot3[6], rot3[7]);
                    }
                    break;
            }
        }

        private void DisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (!isHost)
                is_connected = false;
            MelonLogger.Msg($" [AvatarSync] User disconnected, IP: {peer.EndPoint.Address}, Reason: {disconnectInfo.ToString()}.");
        }

        private void ConnectedEvent(NetPeer peer)
        {
            if (!isHost)
                is_connected = true;
            MelonLogger.Msg($" [AvatarSync] New user connected, IP: {peer.EndPoint.Address}");
        }

        private void ConnectionRequestEvent(ConnectionRequest request)
        {
            MelonLogger.Msg($" [AvatarSync] Connection request from: {request.RemoteEndPoint.Address}");
            var peer = request.AcceptIfKey(token);
            if (peer == null)
            {
                MelonLogger.Msg($" [AvatarSync] Connection request from: {request.RemoteEndPoint.Address} rejected, wrong token.");
            }
        }

        
        public override void OnApplicationQuit()
        {
            m_fingersBends = null;
            m_fingersSpreads = null;
        }

        private static void ResetFingers()
        {
            for (int x = 0; x < 10; x++)
            {
                m_fingersSpreads[x] = 0f;
                m_fingersBends[x] = 0f;
            }
        }


        private void ManualControl()
        {
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                //Reset both hands position
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    rightHandPosition = new Vector3(-120f, 390f, 65.6f);
                    leftHandPosition = new Vector3(120f, 390f, 65.6f);
                }
                //Reset both hands rotation
                else if (Input.GetKeyDown(KeyCode.F2))
                {
                    rightHandRotation = handRightOrginal;
                    leftHandRotation = handLeftOrginal;
                }
                //Reset camera rotation
                else if (Input.GetKeyDown(KeyCode.F3))
                {
                    cameraTransform.rotation = cameraTransformOriginal;
                }
                //Reset hands fingers
                else if (Input.GetKeyDown(KeyCode.F4))
                {
                    ResetFingers();
                }
            }

            //Change speed direction
            if (Input.GetKey(KeyCode.LeftControl))
            {
                spreadSpeed = -0.01f;
            }
            else
            {
                spreadSpeed = 0.01f;
            }
            //Finger 0
            if (Input.GetKey(KeyCode.Alpha5))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[0] = Mathf.Clamp(m_fingersSpreads[0] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[0] = Mathf.Clamp(m_fingersBends[0] + spreadSpeed, 0f, 1f);
            }
            //Finger 1
            if (Input.GetKey(KeyCode.Alpha4))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[1] = Mathf.Clamp(m_fingersSpreads[1] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[1] = Mathf.Clamp(m_fingersBends[1] + spreadSpeed, 0f, 1f);
            }
            //Finger 2
            if (Input.GetKey(KeyCode.Alpha3))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[2] = Mathf.Clamp(m_fingersSpreads[2] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[2] = Mathf.Clamp(m_fingersBends[2] + spreadSpeed, 0f, 1f);
            }
            //Finger 3
            if (Input.GetKey(KeyCode.Alpha2))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[3] = Mathf.Clamp(m_fingersSpreads[3] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[3] = Mathf.Clamp(m_fingersBends[3] + spreadSpeed, 0f, 1f);
            }
            //Finger 4
            if (Input.GetKey(KeyCode.Alpha1))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[4] = Mathf.Clamp(m_fingersSpreads[4] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[4] = Mathf.Clamp(m_fingersBends[4] + spreadSpeed, 0f, 1f);
            }
            //Finger 5
            if (Input.GetKey(KeyCode.Alpha6))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[5] = Mathf.Clamp(m_fingersSpreads[5] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[5] = Mathf.Clamp(m_fingersBends[5] + spreadSpeed, 0f, 1f);
            }
            //Finger 6
            if (Input.GetKey(KeyCode.Alpha7))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[6] = Mathf.Clamp(m_fingersSpreads[6] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[6] = Mathf.Clamp(m_fingersBends[6] + spreadSpeed, 0f, 1f);
            }
            //Finger 7
            if (Input.GetKey(KeyCode.Alpha8))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[7] = Mathf.Clamp(m_fingersSpreads[7] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[7] = Mathf.Clamp(m_fingersBends[7] + spreadSpeed, 0f, 1f);
            }
            //Finger 8
            if (Input.GetKey(KeyCode.Alpha9))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[8] = Mathf.Clamp(m_fingersSpreads[8] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[8] = Mathf.Clamp(m_fingersBends[8] + spreadSpeed, 0f, 1f);
            }
            //Finger 9
            if (Input.GetKey(KeyCode.Alpha0))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    m_fingersSpreads[9] = Mathf.Clamp(m_fingersSpreads[9] + spreadSpeed, 0f, 1f);
                else
                    m_fingersBends[9] = Mathf.Clamp(m_fingersBends[9] + spreadSpeed, 0f, 1f);
            }

            //Rotate hands broken tm
            if (Input.GetMouseButton(3))
            {
                rightHandRotation = new Quaternion(rightHandRotation.x - Input.GetAxis("Mouse X") /2,
                    rightHandRotation.y - Input.GetAxis("Mouse Y") /2,
                    rightHandRotation.z,
                    rightHandRotation.w);
            }

            //Rotate hands broken tm
            if (Input.GetMouseButton(4))
            {
                leftHandRotation = new Quaternion(leftHandRotation.x - Input.GetAxis("Mouse X") /2,
                    leftHandRotation.y - Input.GetAxis("Mouse Y") /2,
                    leftHandRotation.z,
                    leftHandRotation.w);
            }
            //Rotate hands
            if (Input.GetKey(KeyCode.Keypad7))
            {
                rightHandRotation = new Quaternion(rightHandRotation.x, rightHandRotation.y, rightHandRotation.z + moveSpeed, rightHandRotation.w);
                leftHandRotation = new Quaternion(leftHandRotation.x, leftHandRotation.y, leftHandRotation.z + moveSpeed, rightHandRotation.w);
            }
            //Rotate hands
            if (Input.GetKey(KeyCode.Keypad9))
            {
                rightHandRotation = new Quaternion(rightHandRotation.x, rightHandRotation.y, rightHandRotation.z - moveSpeed, rightHandRotation.w);
                leftHandRotation = new Quaternion(leftHandRotation.x, leftHandRotation.y, leftHandRotation.z - moveSpeed, rightHandRotation.w);
            }
            //Change move speed
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                if (Input.GetMouseButton(4))
                    moveSpeedHead = moveSpeedHead - 1f;
                else
                    moveSpeed = moveSpeed - 0.1f;
            }
            //Change move speed
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                if (Input.GetMouseButton(4))
                    moveSpeedHead = moveSpeedHead + 1f;
                else
                    moveSpeed = moveSpeed + 0.1f;
            }
            //Head Rotation
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                cameraTransform.RotateAround(cameraTransform.position, headMoveType == 0 ? cameraTransform.forward
                    : headMoveType == 1 ? cameraTransform.up : -cameraTransform.forward, Input.GetAxis("Mouse X") * moveSpeedHead);
            }
            //Move both hands down
            if (Input.GetKey(KeyCode.DownArrow))
            {
                rightHandPosition = new Vector3(rightHandPosition.x, rightHandPosition.y, rightHandPosition.z + moveSpeed);
                leftHandPosition = new Vector3(leftHandPosition.x, leftHandPosition.y, leftHandPosition.z + moveSpeed);
            }
            //Move both hands up
            if (Input.GetKey(KeyCode.UpArrow))
            {
                rightHandPosition = new Vector3(rightHandPosition.x, rightHandPosition.y, rightHandPosition.z - moveSpeed);
                leftHandPosition = new Vector3(leftHandPosition.x, leftHandPosition.y, leftHandPosition.z - moveSpeed);
            }
            //Move both hands forward
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                rightHandPosition = new Vector3(rightHandPosition.x, rightHandPosition.y + moveSpeed, rightHandPosition.z);
                leftHandPosition = new Vector3(leftHandPosition.x, leftHandPosition.y + moveSpeed, leftHandPosition.z);
            }            
            //Move both hands backwards
            if (Input.GetKey(KeyCode.RightArrow))
            {
                rightHandPosition = new Vector3(rightHandPosition.x, rightHandPosition.y - moveSpeed, rightHandPosition.z);
                leftHandPosition = new Vector3(leftHandPosition.x, leftHandPosition.y - moveSpeed, leftHandPosition.z);
            }
            //Move left hand left
            if (Input.GetKey(KeyCode.Keypad1))
            {
                leftHandPosition = new Vector3(leftHandPosition.x + moveSpeed, leftHandPosition.y, leftHandPosition.z);
            }
            //Move left hand right
            if (Input.GetKey(KeyCode.Keypad3))
            {
                leftHandPosition = new Vector3(leftHandPosition.x - moveSpeed, leftHandPosition.y, leftHandPosition.z);
            }
            //Move right hand left
            if (Input.GetKey(KeyCode.Keypad4))
            {
                rightHandPosition = new Vector3(rightHandPosition.x + moveSpeed, rightHandPosition.y, rightHandPosition.z);
            }
            //Move right hand right
            if (Input.GetKey(KeyCode.Keypad6))
            {
                rightHandPosition = new Vector3(rightHandPosition.x - moveSpeed, rightHandPosition.y, rightHandPosition.z);
            }
        }


        public override void OnUpdate()
        {
            ms_inVrMode = XRDevice.isPresent;
            if (IsManualControl && isReady)
                ManualControl();

            if (isHost && manager != null && avatar_sync)
            {
                if (manager.ConnectedPeersCount != 0)
                {
                    try
                    {
                        NetDataWriter wr = new NetDataWriter();
                        if (l_handController != null)
                        {
                            wr.Put((byte)2);
                            wr.PutArray(l_handController.field_Private_ArrayOf_Single_1);
                            wr.PutArray(l_handController.field_Private_ArrayOf_Single_3);
                            manager.SendToAll(wr, DeliveryMethod.ReliableOrdered);
                        }

                        if (l_solver.leftArm?.target != null && l_solver.rightArm?.target != null)
                        {
                            wr = new NetDataWriter();
                            wr.Put((byte)0);
                            wr.PutArray(new float[]
                            {
                                l_solver.rightArm.position.x,
                                l_solver.rightArm.position.y,
                                l_solver.rightArm.position.z,
                                l_solver.leftArm.position.x,
                                l_solver.leftArm.position.y,
                                l_solver.leftArm.position.z
                            });
                            wr.PutArray(new float[]
                            {
                                l_solver.rightArm.rotation.x,
                                l_solver.rightArm.rotation.y,
                                l_solver.rightArm.rotation.z,
                                l_solver.rightArm.rotation.w,
                                l_solver.leftArm.rotation.x,
                                l_solver.leftArm.rotation.y,
                                l_solver.leftArm.rotation.z,
                                l_solver.leftArm.rotation.w
                            });
                            manager.SendToAll(wr, DeliveryMethod.ReliableOrdered);
                        }
                        if (l_solver.leftLeg != null && l_solver.rightLeg != null)
                        {
                            wr = new NetDataWriter();
                            wr.Put((byte)3);
                            wr.PutArray(new float[]
                            {
                                l_solver.leftLeg.IKPosition.x,
                                l_solver.leftLeg.IKPosition.y,
                                l_solver.leftLeg.IKPosition.z,
                                l_solver.rightLeg.IKPosition.x,
                                l_solver.rightLeg.IKPosition.y,
                                l_solver.rightLeg.IKPosition.z
                            });
                            wr.PutArray(new float[]
                            {
                                l_solver.leftLeg.IKRotation.x,
                                l_solver.leftLeg.IKRotation.y,
                                l_solver.leftLeg.IKRotation.z,
                                l_solver.leftLeg.IKRotation.w,
                                l_solver.rightLeg.IKRotation.x,
                                l_solver.rightLeg.IKRotation.y,
                                l_solver.rightLeg.IKRotation.z,
                                l_solver.rightLeg.IKRotation.w
                            });
                            manager.SendToAll(wr, DeliveryMethod.ReliableOrdered);
                        }

                        wr = new NetDataWriter();
                        wr.Put((byte)1);
                        wr.PutArray(new float[]
                        {
                            cameraTransform.localPosition.x,
                            cameraTransform.localPosition.y,
                            cameraTransform.localPosition.z
                        });
                        wr.PutArray(new float[]
                        {
                            cameraTransform.rotation.x,
                            cameraTransform.rotation.y,
                            cameraTransform.rotation.z,
                            cameraTransform.rotation.w
                        });
                        manager.SendToAll(wr, DeliveryMethod.ReliableOrdered);
                    }
                    catch (Exception) { }
                }
            }
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.F5))
                    IsManualControl = !IsManualControl;
                else if (Input.GetKeyDown(KeyCode.F6))
                {
                    if (!isReady)
                    {
                        isReady = true;
                        rightHandPosition = new Vector3(-120f, 390f, 65.6f);
                        leftHandPosition = new Vector3(120f, 390f, 65.6f);
                    }
                    else
                    {
                        ResetFingers();
                        isReady = false;
                        cameraTransform.rotation = cameraTransformOriginal;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.F9) && avatar_sync)
                {
                    isHost = !isHost;
                    MelonLogger.Msg(" [AvatarSync] Current avatar sync mode \"" + (isHost ? "HOST" : "CLIENT") + "\", change via F9 key.");
                }
                else if (Input.GetKeyDown(KeyCode.F10) && avatar_sync)
                {
                    if (manager != null)
                    {
                        MelonLogger.Msg($" [AvatarSync] Killing current connection manager.");
                        manager.DisconnectAll();
                        manager = null;
                    }
                    if (isHost)
                    {
                        manager = new NetManager(listener);
                        manager.Start(host_port);
                    }
                    else
                    {
                        if (manager == null)
                            manager = new NetManager(listener);
                        manager.Connect(host_ip, host_port, token);
                    }
                }
            }
            if (isReady)
            {
                var l_expParams = VRCPlayer.field_Internal_Static_VRCPlayer_0?.prop_VRCAvatarManager_0?.prop_VRCAvatarDescriptor_0?.expressionParameters?.parameters;
                var l_playableController = VRCPlayer.field_Internal_Static_VRCPlayer_0?.field_Private_AnimatorControllerManager_0?.field_Private_AvatarAnimParamController_0?.field_Private_AvatarPlayableController_0;
                if ((l_expParams != null) && (l_playableController != null))
                {
                    for (int i = 0; i < l_expParams.Length; i++)
                    {
                        var l_expParam = l_expParams[i];
                        if (l_expParam.name.StartsWith("_FingerValue") && (l_expParam.valueType == VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float))
                        {
                            int l_bufferIndex = -1;
                            if (Int32.TryParse(l_expParam.name.Substring(12), out l_bufferIndex))
                            {
                                if ((l_bufferIndex >= 0) && (l_bufferIndex <= 9))
                                {
                                    l_playableController.Method_Public_Boolean_Int32_Single_1(i, m_fingersBends[l_bufferIndex]);
                                }
                            }
                            continue;
                        }
                        if (l_expParam.name.StartsWith("_HandPresent") && (l_expParam.valueType == VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool))
                        {
                            int l_bufferIndex = -1;
                            if (Int32.TryParse(l_expParam.name.Substring(12), out l_bufferIndex))
                            {
                                if ((l_bufferIndex >= 0) && (l_bufferIndex <= 1))
                                {
                                    l_playableController.Method_Public_Boolean_Int32_Single_1(i, true ? 1.0f : 0.0f); // Fallback
                                }
                            }
                            continue;
                        }
                    }
                }


                var l_solver = VRCPlayer.field_Internal_Static_VRCPlayer_0?.field_Private_VRC_AnimationController_0?.field_Private_VRIK_0?.solver;
                if (l_solver != null)
                {
                    if (l_solver.leftArm?.target != null)
                    {
                        Vector3 l_newPos = new Vector3(leftHandPosition.x, leftHandPosition.y, -leftHandPosition.z) * 0.001f;
                        Quaternion l_newRot = new Quaternion(-leftHandRotation.x, -leftHandRotation.y, leftHandRotation.z, leftHandRotation.w);
                        ApplyAdjustment(ref l_newPos, ref l_newRot);

                        Transform l_rootTransform = GetRootTransform(ref l_solver);
                        m_leftTargetPosition = l_rootTransform.position + l_rootTransform.rotation * l_newPos;
                        m_leftTargetRotation = l_rootTransform.rotation * l_newRot;

                        var l_pickupJoint = VRCPlayer.field_Internal_Static_VRCPlayer_0?.field_Private_VRC_AnimationController_0?.field_Private_IkController_0?.field_Private_VRCHandGrasper_0?.field_Private_GameObject_0;
                        if (l_pickupJoint != null)
                        {
                            if (handLeftOrginal == null)
                                handLeftOrginal = l_pickupJoint.transform.rotation;
                            l_pickupJoint.transform.position = m_leftTargetPosition;
                            l_pickupJoint.transform.rotation = m_leftTargetRotation;
                        }
                    }
                    if (l_solver.rightArm?.target != null)
                    {
                        Vector3 l_newPos = new Vector3(rightHandPosition.x, rightHandPosition.y, -rightHandPosition.z) * 0.001f;
                        Quaternion l_newRot = new Quaternion(-rightHandRotation.x, -rightHandRotation.y, rightHandRotation.z, rightHandRotation.w);
                        ApplyAdjustment(ref l_newPos, ref l_newRot);

                        Transform l_rootTransform = GetRootTransform(ref l_solver);
                        m_rightTargetPosition = l_rootTransform.position + l_rootTransform.rotation * l_newPos;
                        m_rightTargetRotation = l_rootTransform.rotation * l_newRot;
                        var l_pickupJoint = VRCPlayer.field_Internal_Static_VRCPlayer_0?.field_Private_VRC_AnimationController_0?.field_Private_IkController_0?.field_Private_VRCHandGrasper_1?.field_Private_GameObject_0;
                        if (l_pickupJoint != null)
                        {
                            if (handRightOrginal == null)
                                handRightOrginal = l_pickupJoint.transform.rotation;
                            l_pickupJoint.transform.position = m_rightTargetPosition;
                            l_pickupJoint.transform.rotation = m_rightTargetRotation;
                        }
                    }
                }

                var l_handController = VRCPlayer.field_Internal_Static_VRCPlayer_0?.field_Private_VRC_AnimationController_0?.field_Private_HandGestureController_0;
                if (l_handController != null)
                {
                    l_handController.field_Internal_Boolean_0 = true;
                    l_handController.field_Private_EnumNPublicSealedvaKeMoCoGaViOcViDaWaUnique_0 = VRCInputManager.EnumNPublicSealedvaKeMoCoGaViOcViDaWaUnique.Index;

                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            int l_dataIndex = i * 5 + j;
                            l_handController.field_Private_ArrayOf_VRCInput_0[l_dataIndex].field_Public_Single_0 = 1.0f - m_fingersBends[l_dataIndex]; // Squeeze
                            l_handController.field_Private_ArrayOf_VRCInput_1[l_dataIndex].field_Public_Single_0 = m_fingersSpreads[l_dataIndex]; // Spread
                        }
                    }
                }
            }
        }

        static IKSolverVR l_solver = null;
        static HandGestureController l_handController = null;

        public override void OnLateUpdate()
        {
            if (isReady)
            {
                l_solver = VRCPlayer.field_Internal_Static_VRCPlayer_0?.field_Private_VRC_AnimationController_0?.field_Private_VRIK_0?.solver;
                if (l_solver != null)
                {
                    if (l_solver.leftArm?.target != null)
                    {
                        l_solver.leftArm.positionWeight = 1f;
                        l_solver.leftArm.rotationWeight = 1f;
                        l_solver.leftArm.target.position = m_leftTargetPosition;
                        l_solver.leftArm.target.rotation = m_leftTargetRotation;
                    }


                    if (l_solver.rightArm?.target != null)
                    {
                        l_solver.rightArm.positionWeight = 1f;
                        l_solver.rightArm.rotationWeight = 1f;
                        l_solver.rightArm.target.position = m_rightTargetPosition;
                        l_solver.rightArm.target.rotation = m_rightTargetRotation;
                    }
                }

                l_handController = VRCPlayer.field_Internal_Static_VRCPlayer_0?.field_Private_VRC_AnimationController_0?.field_Private_HandGestureController_0;
                if (l_handController != null)
                {
                    l_handController.field_Internal_Boolean_0 = true;
                    l_handController.field_Private_EnumNPublicSealedvaKeMoCoGaViOcViDaWaUnique_0 = VRCInputManager.EnumNPublicSealedvaKeMoCoGaViOcViDaWaUnique.Index;
                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            int l_dataIndex = i * 5 + j;
                            l_handController.field_Private_ArrayOf_Single_1[l_dataIndex] = 1.0f - m_fingersBends[l_dataIndex]; // Squeeze
                            l_handController.field_Private_ArrayOf_Single_3[l_dataIndex] = m_fingersSpreads[l_dataIndex]; // Spread
                        }
                    }
                }
            }
        }



        void ApplyAdjustment(ref Vector3 pos, ref Quaternion rot)
        {

            pos.x *= -1f;
            Swap(ref pos.y, ref pos.z);
            rot = c_hmdRotationFix * rot;

            // Easy way to scale, but can be improved (but how?)
            var l_height = VRCTrackingManager.Method_Public_Static_Single_5();
            pos += m_rootOffset;

            pos.y -= m_rootOffset.y * 1f;
            pos.z -= m_rootOffset.z * 2f;
            pos *= l_height;
        }

        Transform GetRootTransform(ref RootMotion.FinalIK.IKSolverVR solver)
        {
            Transform l_result = null;

            l_result = solver.spine?.headTarget?.transform?.parent;
            if (l_result == null)
                l_result = VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
            return l_result;
        }

        static bool VRCIM_ControllersType(ref bool __result, VRCInputManager.EnumNPublicSealedvaKeMoCoGaViOcViDaWaUnique __0)
        {
            if (ms_inVrMode && isReady)
            {
                if (__0 == VRCInputManager.EnumNPublicSealedvaKeMoCoGaViOcViDaWaUnique.Index)
                {
                    __result = true;
                    return false;
                }
                else
                {
                    __result = false;
                    return false;
                }
            }
            else
                return true;
        }

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}