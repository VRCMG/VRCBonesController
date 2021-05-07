using Il2CppSystem.Reflection;
using System;
using System.Linq;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace VRCBonesController
{
    public class MainClass : MelonLoader.MelonMod
    {

        readonly Quaternion c_hmdRotationFix = new Quaternion(0f, 0.7071068f, 0.7071068f, 0f);

        Vector3 m_rootOffset = new Vector3(0f, 0.5f, 0.25f);

        static bool ms_inVrMode = false;
        float[] m_fingersBends = null;
        float[] m_fingersSpreads = null;

        Vector3 m_leftTargetPosition;
        Quaternion m_leftTargetRotation;
        Vector3 m_rightTargetPosition;
        Quaternion m_rightTargetRotation;

        Vector3 rightHandPosition = new Vector3(-120f, 390f, 65.6f);
        Vector3 leftHandPosition = new Vector3(120f, 390f, 65.6f);

        Quaternion rightHandRotation = new Quaternion(0f, 0f, 0f, 0f);
        Quaternion leftHandRotation = new Quaternion(0f, 0f, 0f, 0f);

        Quaternion cameraTransformOriginal;

        Quaternion handLeftOrginal;
        Quaternion handRightOrginal;

        Transform cameraTransform = null;

        static bool isReady = false;
        int headMoveType = 0;

        float moveSpeed = 0.1f;
        float moveSpeedHead = 3f;

        float spreadSpeed = 0.01f;


        public bool IsManualControl = false;

        public override void OnApplicationStart()
        {
            ms_inVrMode = VRCTrackingManager.Method_Public_Static_Boolean_4();
            m_fingersBends = new float[10];
            m_fingersSpreads = new float[10];

            // Patches
            var l_patchMethod = new Harmony.HarmonyMethod(typeof(MainClass), "VRCIM_ControllersType");
            typeof(VRCInputManager).GetMethods().Where(x =>
                    x.Name.StartsWith("Method_Public_Static_Boolean_EnumNPublicSealedvaKeMoCoGaViOcViDaWaUnique_")
                ).ToList().ForEach(m => Harmony.Patch(m, l_patchMethod));

            OnPreferencesSaved();
        }



        public override void VRChat_OnUiManagerInit()
        {
            base.VRChat_OnUiManagerInit();
            var camera = UnityEngine.Object.FindObjectOfType<VRCVrCamera>();
            var transform = camera.GetIl2CppType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == Il2CppType.Of<Transform>()).ToArray()[0];
            cameraTransform = transform.GetValue(camera).Cast<Transform>();
            cameraTransformOriginal = cameraTransform.rotation;
        }
        
        public override void OnApplicationQuit()
        {
            m_fingersBends = null;
            m_fingersSpreads = null;
        }

        private void ResetFingers()
        {
            for (int x = 0; x < 10; x++)
            {
                m_fingersSpreads[x] = 0f;
                m_fingersBends[x] = 0f;
            }
        }


        private void ManualControl()
        {
            //Reset both hands position
            if (Input.GetKey(KeyCode.F1))
            {
                rightHandPosition = new Vector3(-120f, 390f, 65.6f);
                leftHandPosition = new Vector3(120f, 390f, 65.6f);
            }
            //Reset both hands rotation
            if (Input.GetKey(KeyCode.F2))
            {
                rightHandRotation = handRightOrginal;
                leftHandRotation = handLeftOrginal;
            }
            //Reset camera rotation
            if (Input.GetKey(KeyCode.F3))
            {
                cameraTransform.rotation = cameraTransformOriginal;
            }
            //Reset hands fingers
            if (Input.GetKey(KeyCode.F4))
            {
                ResetFingers();
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
            if (Input.GetMouseButton(1) && Input.GetMouseButton(0))
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
            ms_inVrMode = VRCTrackingManager.Method_Public_Static_Boolean_4();
            if (IsManualControl && isReady)
                ManualControl();
            if (Input.GetKeyDown(KeyCode.F5))
                IsManualControl = !IsManualControl;
            if (Input.GetKeyDown(KeyCode.F6))
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
                        if (true)
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
        }

        public override void OnLateUpdate()
        {
            if (isReady)
            {
                var l_solver = VRCPlayer.field_Internal_Static_VRCPlayer_0?.field_Private_VRC_AnimationController_0?.field_Private_VRIK_0?.solver;
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