using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Valve.VR
{
	public class SteamVR_Action_Pose_Source : SteamVR_Action_In_Source, ISteamVR_Action_Pose, ISteamVR_Action_In_Source, ISteamVR_Action_Source
	{
		public ETrackingUniverseOrigin universeOrigin = ETrackingUniverseOrigin.TrackingUniverseRawAndUncalibrated;

		protected static uint poseActionData_size = 0u;

		public float changeTolerance = Mathf.Epsilon;

		protected InputPoseActionData_t poseActionData;

		protected InputPoseActionData_t lastPoseActionData;

		protected InputPoseActionData_t tempPoseActionData;

		protected SteamVR_Action_Pose poseAction;

		public static float framesAhead = 2f;

		public override bool changed { get; protected set; }

		public override bool lastChanged { get; protected set; }

		public override ulong activeOrigin
		{
			get
			{
				if (active)
				{
					return poseActionData.activeOrigin;
				}
				return 0uL;
			}
		}

		public override ulong lastActiveOrigin => lastPoseActionData.activeOrigin;

		public override bool active
		{
			get
			{
				if (activeBinding)
				{
					return action.actionSet.IsActive(base.inputSource);
				}
				return false;
			}
		}

		public override bool activeBinding => poseActionData.bActive;

		public override bool lastActive { get; protected set; }

		public override bool lastActiveBinding => lastPoseActionData.bActive;

		public ETrackingResult trackingState => poseActionData.pose.eTrackingResult;

		public ETrackingResult lastTrackingState => lastPoseActionData.pose.eTrackingResult;

		public bool poseIsValid => poseActionData.pose.bPoseIsValid;

		public bool lastPoseIsValid => lastPoseActionData.pose.bPoseIsValid;

		public bool deviceIsConnected => poseActionData.pose.bDeviceIsConnected;

		public bool lastDeviceIsConnected => lastPoseActionData.pose.bDeviceIsConnected;

		public Vector3 localPosition { get; protected set; }

		public Quaternion localRotation { get; protected set; }

		public Vector3 lastLocalPosition { get; protected set; }

		public Quaternion lastLocalRotation { get; protected set; }

		public Vector3 velocity { get; protected set; }

		public Vector3 lastVelocity { get; protected set; }

		public Vector3 angularVelocity { get; protected set; }

		public Vector3 lastAngularVelocity { get; protected set; }

		public event SteamVR_Action_Pose.ActiveChangeHandler onActiveChange;

		public event SteamVR_Action_Pose.ActiveChangeHandler onActiveBindingChange;

		public event SteamVR_Action_Pose.ChangeHandler onChange;

		public event SteamVR_Action_Pose.UpdateHandler onUpdate;

		public event SteamVR_Action_Pose.TrackingChangeHandler onTrackingChanged;

		public event SteamVR_Action_Pose.ValidPoseChangeHandler onValidPoseChanged;

		public event SteamVR_Action_Pose.DeviceConnectedChangeHandler onDeviceConnectedChanged;

		public override void Preinitialize(SteamVR_Action wrappingAction, SteamVR_Input_Sources forInputSource)
		{
			base.Preinitialize(wrappingAction, forInputSource);
			poseAction = wrappingAction as SteamVR_Action_Pose;
		}

		public override void Initialize()
		{
			base.Initialize();
			if (poseActionData_size == 0)
			{
				poseActionData_size = (uint)Marshal.SizeOf(typeof(InputPoseActionData_t));
			}
		}

		public virtual void RemoveAllListeners()
		{
			Delegate[] invocationList;
			if (this.onActiveChange != null)
			{
				invocationList = this.onActiveChange.GetInvocationList();
				if (invocationList != null)
				{
					Delegate[] array = invocationList;
					foreach (Delegate @delegate in array)
					{
						onActiveChange -= (SteamVR_Action_Pose.ActiveChangeHandler)@delegate;
					}
				}
			}
			if (this.onChange != null)
			{
				invocationList = this.onChange.GetInvocationList();
				if (invocationList != null)
				{
					Delegate[] array = invocationList;
					foreach (Delegate delegate2 in array)
					{
						onChange -= (SteamVR_Action_Pose.ChangeHandler)delegate2;
					}
				}
			}
			if (this.onUpdate != null)
			{
				invocationList = this.onUpdate.GetInvocationList();
				if (invocationList != null)
				{
					Delegate[] array = invocationList;
					foreach (Delegate delegate3 in array)
					{
						onUpdate -= (SteamVR_Action_Pose.UpdateHandler)delegate3;
					}
				}
			}
			if (this.onTrackingChanged != null)
			{
				invocationList = this.onTrackingChanged.GetInvocationList();
				if (invocationList != null)
				{
					Delegate[] array = invocationList;
					foreach (Delegate delegate4 in array)
					{
						onTrackingChanged -= (SteamVR_Action_Pose.TrackingChangeHandler)delegate4;
					}
				}
			}
			if (this.onValidPoseChanged != null)
			{
				invocationList = this.onValidPoseChanged.GetInvocationList();
				if (invocationList != null)
				{
					Delegate[] array = invocationList;
					foreach (Delegate delegate5 in array)
					{
						onValidPoseChanged -= (SteamVR_Action_Pose.ValidPoseChangeHandler)delegate5;
					}
				}
			}
			if (this.onDeviceConnectedChanged == null)
			{
				return;
			}
			invocationList = this.onDeviceConnectedChanged.GetInvocationList();
			if (invocationList != null)
			{
				Delegate[] array = invocationList;
				foreach (Delegate delegate6 in array)
				{
					onDeviceConnectedChanged -= (SteamVR_Action_Pose.DeviceConnectedChangeHandler)delegate6;
				}
			}
		}

		public override void UpdateValue()
		{
			UpdateValue(false);
		}

		public virtual void UpdateValue(bool skipStateAndEventUpdates)
		{
			lastChanged = changed;
			lastPoseActionData = poseActionData;
			lastLocalPosition = localPosition;
			lastLocalRotation = localRotation;
			lastVelocity = velocity;
			lastAngularVelocity = angularVelocity;
			EVRInputError eVRInputError = ((framesAhead != 0f) ? OpenVR.Input.GetPoseActionDataRelativeToNow(base.handle, universeOrigin, framesAhead * (Time.timeScale / SteamVR.instance.hmd_DisplayFrequency), ref poseActionData, poseActionData_size, inputSourceHandle) : OpenVR.Input.GetPoseActionDataForNextFrame(base.handle, universeOrigin, ref poseActionData, poseActionData_size, inputSourceHandle));
			if (eVRInputError != 0)
			{
				Debug.LogError("<b>[SteamVR]</b> GetPoseActionData error (" + base.fullPath + "): " + eVRInputError.ToString() + " Handle: " + base.handle + ". Input source: " + base.inputSource);
			}
			if (active)
			{
				SetCacheVariables();
				changed = GetChanged();
			}
			if (changed)
			{
				base.changedTime = base.updateTime;
			}
			if (!skipStateAndEventUpdates)
			{
				CheckAndSendEvents();
			}
		}

		protected void SetCacheVariables()
		{
			localPosition = poseActionData.pose.mDeviceToAbsoluteTracking.GetPosition();
			localRotation = poseActionData.pose.mDeviceToAbsoluteTracking.GetRotation();
			velocity = GetUnityCoordinateVelocity(poseActionData.pose.vVelocity);
			angularVelocity = GetUnityCoordinateAngularVelocity(poseActionData.pose.vAngularVelocity);
			base.updateTime = Time.realtimeSinceStartup;
		}

		protected bool GetChanged()
		{
			if (Vector3.Distance(localPosition, lastLocalPosition) > changeTolerance)
			{
				return true;
			}
			if (Mathf.Abs(Quaternion.Angle(localRotation, lastLocalRotation)) > changeTolerance)
			{
				return true;
			}
			if (Vector3.Distance(velocity, lastVelocity) > changeTolerance)
			{
				return true;
			}
			if (Vector3.Distance(angularVelocity, lastAngularVelocity) > changeTolerance)
			{
				return true;
			}
			return false;
		}

		public bool GetVelocitiesAtTimeOffset(float secondsFromNow, out Vector3 velocityAtTime, out Vector3 angularVelocityAtTime)
		{
			EVRInputError poseActionDataRelativeToNow = OpenVR.Input.GetPoseActionDataRelativeToNow(base.handle, universeOrigin, secondsFromNow, ref tempPoseActionData, poseActionData_size, inputSourceHandle);
			if (poseActionDataRelativeToNow != 0)
			{
				Debug.LogError("<b>[SteamVR]</b> GetPoseActionData error (" + base.fullPath + "): " + poseActionDataRelativeToNow.ToString() + " handle: " + base.handle);
				velocityAtTime = Vector3.zero;
				angularVelocityAtTime = Vector3.zero;
				return false;
			}
			velocityAtTime = GetUnityCoordinateVelocity(tempPoseActionData.pose.vVelocity);
			angularVelocityAtTime = GetUnityCoordinateAngularVelocity(tempPoseActionData.pose.vAngularVelocity);
			return true;
		}

		public bool GetPoseAtTimeOffset(float secondsFromNow, out Vector3 positionAtTime, out Quaternion rotationAtTime, out Vector3 velocityAtTime, out Vector3 angularVelocityAtTime)
		{
			EVRInputError poseActionDataRelativeToNow = OpenVR.Input.GetPoseActionDataRelativeToNow(base.handle, universeOrigin, secondsFromNow, ref tempPoseActionData, poseActionData_size, inputSourceHandle);
			if (poseActionDataRelativeToNow != 0)
			{
				Debug.LogError("<b>[SteamVR]</b> GetPoseActionData error (" + base.fullPath + "): " + poseActionDataRelativeToNow.ToString() + " handle: " + base.handle);
				velocityAtTime = Vector3.zero;
				angularVelocityAtTime = Vector3.zero;
				positionAtTime = Vector3.zero;
				rotationAtTime = Quaternion.identity;
				return false;
			}
			velocityAtTime = GetUnityCoordinateVelocity(tempPoseActionData.pose.vVelocity);
			angularVelocityAtTime = GetUnityCoordinateAngularVelocity(tempPoseActionData.pose.vAngularVelocity);
			positionAtTime = tempPoseActionData.pose.mDeviceToAbsoluteTracking.GetPosition();
			rotationAtTime = tempPoseActionData.pose.mDeviceToAbsoluteTracking.GetRotation();
			return true;
		}

		public void UpdateTransform(Transform transformToUpdate)
		{
			transformToUpdate.localPosition = localPosition;
			transformToUpdate.localRotation = localRotation;
		}

		protected virtual void CheckAndSendEvents()
		{
			if (trackingState != lastTrackingState && this.onTrackingChanged != null)
			{
				this.onTrackingChanged(poseAction, base.inputSource, trackingState);
			}
			if (poseIsValid != lastPoseIsValid && this.onValidPoseChanged != null)
			{
				this.onValidPoseChanged(poseAction, base.inputSource, poseIsValid);
			}
			if (deviceIsConnected != lastDeviceIsConnected && this.onDeviceConnectedChanged != null)
			{
				this.onDeviceConnectedChanged(poseAction, base.inputSource, deviceIsConnected);
			}
			if (changed && this.onChange != null)
			{
				this.onChange(poseAction, base.inputSource);
			}
			if (active != lastActive && this.onActiveChange != null)
			{
				this.onActiveChange(poseAction, base.inputSource, active);
			}
			if (activeBinding != lastActiveBinding && this.onActiveBindingChange != null)
			{
				this.onActiveBindingChange(poseAction, base.inputSource, activeBinding);
			}
			if (this.onUpdate != null)
			{
				this.onUpdate(poseAction, base.inputSource);
			}
		}

		protected Vector3 GetUnityCoordinateVelocity(HmdVector3_t vector)
		{
			return GetUnityCoordinateVelocity(vector.v0, vector.v1, vector.v2);
		}

		protected Vector3 GetUnityCoordinateAngularVelocity(HmdVector3_t vector)
		{
			return GetUnityCoordinateAngularVelocity(vector.v0, vector.v1, vector.v2);
		}

		protected Vector3 GetUnityCoordinateVelocity(float x, float y, float z)
		{
			Vector3 result = default(Vector3);
			result.x = x;
			result.y = y;
			result.z = 0f - z;
			return result;
		}

		protected Vector3 GetUnityCoordinateAngularVelocity(float x, float y, float z)
		{
			Vector3 result = default(Vector3);
			result.x = 0f - x;
			result.y = 0f - y;
			result.z = z;
			return result;
		}
	}
}
