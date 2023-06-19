using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using VRGIN.Controls.Handlers;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Controls
{
	public abstract class Controller : ProtectedBehaviour
	{
		public class Lock
		{
			private Controller _Controller;

			public static readonly Lock Invalid = new Lock();

			public bool IsInvalidating { get; private set; }

			public bool IsValid { get; private set; }

			private Lock()
			{
				IsValid = false;
			}

			internal Lock(Controller controller)
			{
				IsValid = true;
				_Controller = controller;
				_Controller._Lock = this;
				_Controller.OnLock();
			}

			public void Release()
			{
				if (IsValid)
				{
					_Controller._Lock = null;
					_Controller.OnUnlock();
					IsValid = false;
				}
				else
				{
					VRLog.Warn("Tried to release an invalid lock!");
				}
			}

			public void SafeRelease()
			{
				if (IsValid)
				{
					IsInvalidating = true;
				}
				else
				{
					VRLog.Warn("Tried to release an invalid lock!");
				}
			}
		}

		private bool _Started;

		public SteamVR_Behaviour_Pose Tracking;

		private DeviceLegacyAdapter _Input;

		protected BoxCollider Collider;

		private float? appButtonPressTime;

		public List<Tool> Tools = new List<Tool>();

		public Controller Other;

		private const float APP_BUTTON_TIME_THRESHOLD = 0.5f;

		private bool helpShown;

		private List<HelpText> helpTexts;

		private Canvas _Canvas;

		private Lock _Lock = Lock.Invalid;

		private GameObject _AlphaConcealer;

		public SteamVR_RenderModel Model { get; private set; }

		public RumbleManager Rumble { get; private set; }

		public virtual int ToolIndex { get; set; }

		public Tool ActiveTool
		{
			get
			{
				if (ToolIndex >= Tools.Count)
				{
					return null;
				}
				return Tools[ToolIndex];
			}
		}

		public virtual IList<Type> ToolTypes => new List<Type>();

		public bool ToolEnabled
		{
			get
			{
				if (ActiveTool != null)
				{
					return ActiveTool.enabled;
				}
				return false;
			}
			set
			{
				if (ActiveTool != null)
				{
					ActiveTool.enabled = value;
					if (!value)
					{
						HideHelp();
					}
				}
			}
		}

		public bool IsTracking
		{
			get
			{
				if ((bool)Tracking)
				{
					return Tracking.isValid;
				}
				return false;
			}
		}

		public DeviceLegacyAdapter Input => _Input;

		public SteamVR_Input_Sources InputSources
		{
			get
			{
				if ((bool)Tracking)
				{
					return Tracking.inputSource;
				}
				throw new NullReferenceException("Tracking is null");
			}
			set
			{
				if ((bool)Tracking)
				{
					Tracking.inputSource = value;
				}
			}
		}

		[Obsolete("Use TryAcquireFocus() or AcquireFocus()")]
		public bool AcquireFocus(out Lock lockObj)
		{
			return TryAcquireFocus(out lockObj);
		}

		public bool TryAcquireFocus(out Lock lockObj)
		{
			lockObj = null;
			if (CanAcquireFocus())
			{
				lockObj = new Lock(this);
				return true;
			}
			return false;
		}

		public Lock AcquireFocus()
		{
			if (TryAcquireFocus(out var lockObj))
			{
				return lockObj;
			}
			return Lock.Invalid;
		}

		public bool CanAcquireFocus()
		{
			if (_Lock != null)
			{
				return !_Lock.IsValid;
			}
			return true;
		}

		protected virtual void OnLock()
		{
			ToolEnabled = false;
			_AlphaConcealer.SetActive(false);
		}

		protected virtual void OnUnlock()
		{
			ToolEnabled = true;
			_AlphaConcealer.SetActive(true);
		}

		protected virtual void OnDestroy()
		{
			VRLog.Info(" Controller OnDestroy called. " + base.gameObject.name);
			SteamVR_Events.RenderModelLoaded.Remove(_OnRenderModelLoaded);
		}

		protected void SetUp()
		{
			SteamVR_Events.RenderModelLoaded.Listen(_OnRenderModelLoaded);
			Tracking = base.gameObject.AddComponent<SteamVR_Behaviour_Pose>();
			_Input = new DeviceLegacyAdapter(Tracking);
			Rumble = base.gameObject.AddComponent<RumbleManager>();
			base.gameObject.AddComponent<BodyRumbleHandler>();
			base.gameObject.AddComponent<MenuHandler>();
			Model = new GameObject("Model").AddComponent<SteamVR_RenderModel>();
			Model.shader = VRManager.Instance.Context.Materials.StandardShader;
			if (!Model.shader)
			{
				VRLog.Warn("Shader not found");
			}
			Model.transform.SetParent(base.transform, false);
			Model.transform.localPosition = Vector3.zero;
			Model.transform.localRotation = Quaternion.identity;
			Model.gameObject.layer = 0;
			BuildCanvas();
			Collider = new GameObject("Collider").AddComponent<BoxCollider>();
			Collider.transform.SetParent(base.transform, false);
			Collider.center = new Vector3(0f, -0.02f, -0.06f);
			Collider.size = new Vector3(0.05f, 0.05f, 0.2f);
			Collider.isTrigger = true;
			base.gameObject.AddComponent<Rigidbody>().isKinematic = true;
		}

		private void _OnRenderModelLoaded(SteamVR_RenderModel model, bool isLoaded)
		{
			try
			{
				if ((bool)model && model.transform.IsChildOf(base.transform))
				{
					VRLog.Info("Render model loaded! rendeModelName: '" + model.renderModelName + "'");
					base.gameObject.SendMessageToAll("OnRenderModelLoaded");
					OnRenderModelLoaded();
				}
			}
			catch (Exception obj)
			{
				VRLog.Error(obj);
			}
		}

		private void OnRenderModelLoaded()
		{
			Renderer[] componentsInChildren = Model.GetComponentsInChildren<Renderer>(true);
			foreach (Renderer renderer in componentsInChildren)
			{
				VRLog.Debug($"Name: {renderer.gameObject.name}, Layer: {LayerMask.LayerToName(renderer.gameObject.layer)}, Visible: {renderer.isVisible}, Shader: {renderer.sharedMaterial.shader}");
			}
		}

		protected override void OnAwake()
		{
			base.OnAwake();
			SetUp();
		}

		public void AddTool(Type toolType)
		{
			if (toolType.IsSubclassOf(typeof(Tool)) && !Tools.Any((Tool tool) => toolType.IsAssignableFrom(tool.GetType())))
			{
				Tool tool2 = base.gameObject.AddComponent(toolType) as Tool;
				Tools.Add(tool2);
				CreateToolCanvas(tool2);
				tool2.enabled = false;
			}
		}

		public void AddTool<T>() where T : Tool
		{
			AddTool(typeof(T));
		}

		protected override void OnStart()
		{
			int num = 0;
			foreach (Tool tool in Tools)
			{
				if (num++ != ToolIndex && (bool)tool)
				{
					tool.enabled = false;
					VRLog.Info("Disable tool #{0} ({1})", num - 1, ToolIndex);
					continue;
				}
				VRLog.Info("Enable Tool #{0}", num - 1);
				if (tool.enabled)
				{
					tool.enabled = false;
				}
				tool.enabled = true;
			}
			_Started = true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (!Tracking)
			{
				return;
			}
			_ = InputSources;
			if (_Lock != null && _Lock.IsInvalidating)
			{
				TryReleaseLock();
			}
			if (_Lock != null && _Lock.IsValid)
			{
				return;
			}
			if (Input.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu))
			{
				appButtonPressTime = Time.unscaledTime;
			}
			if (Input.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.unscaledTime - appButtonPressTime > 0.5f)
			{
				ShowHelp();
				appButtonPressTime = null;
			}
			if (!Input.GetPressUp(EVRButtonId.k_EButton_ApplicationMenu))
			{
				return;
			}
			if (helpShown)
			{
				HideHelp();
			}
			else
			{
				if ((bool)ActiveTool)
				{
					ActiveTool.enabled = false;
				}
				ToolIndex = (ToolIndex + 1) % Tools.Count;
				if ((bool)ActiveTool)
				{
					ActiveTool.enabled = true;
				}
			}
			appButtonPressTime = null;
		}

		private void TryReleaseLock()
		{
			DeviceLegacyAdapter input = Input;
			foreach (EVRButtonId item in Enum.GetValues(typeof(EVRButtonId)).OfType<EVRButtonId>())
			{
				if (input.GetPress(item))
				{
					return;
				}
			}
			_Lock.Release();
		}

		public void StartRumble(IRumbleSession session)
		{
			Rumble.StartRumble(session);
		}

		public void StopRumble(IRumbleSession session)
		{
			Rumble.StopRumble(session);
		}

		private void HideHelp()
		{
			if (helpShown)
			{
				helpTexts.ForEach(delegate(HelpText h)
				{
					UnityEngine.Object.Destroy(h.gameObject);
				});
				helpShown = false;
			}
		}

		private void ShowHelp()
		{
			if (ActiveTool != null)
			{
				helpTexts = ActiveTool.GetHelpTexts();
				helpShown = true;
			}
		}

		private void BuildCanvas()
		{
			Canvas canvas = (_Canvas = new GameObject("ToolIconCanvas").AddComponent<Canvas>());
			canvas.renderMode = RenderMode.WorldSpace;
			canvas.transform.SetParent(base.transform, false);
			canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 950f);
			canvas.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 950f);
			canvas.transform.localPosition = new Vector3(0f, -0.02725995f, 0.0279f);
			canvas.transform.localRotation = Quaternion.Euler(30f, 180f, 180f);
			canvas.transform.localScale = new Vector3(4.930151E-05f, 4.930148E-05f, 0f);
			canvas.gameObject.layer = 0;
			_AlphaConcealer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			_AlphaConcealer.transform.SetParent(base.transform, false);
			_AlphaConcealer.transform.localScale = new Vector3(0.05f, 0f, 0.05f);
			_AlphaConcealer.transform.localPosition = new Vector3(0f, -0.0303f, 0.0142f);
			_AlphaConcealer.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
			_AlphaConcealer.GetComponent<Collider>().enabled = false;
		}

		private void CreateToolCanvas(Tool tool)
		{
			Image image = new GameObject("ToolCanvas").AddComponent<Image>();
			image.transform.SetParent(_Canvas.transform, false);
			Texture2D image2 = tool.Image;
			image.sprite = Sprite.Create(image2, new Rect(0f, 0f, image2.width, image2.height), new Vector2(0.5f, 0.5f));
			image.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
			image.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
			image.color = Color.cyan;
			tool.Icon = image.gameObject;
			tool.Icon.SetActive(false);
			tool.Icon.layer = 0;
		}

		public Transform FindAttachPosition(params string[] names)
		{
			Transform transform = (from t in base.transform.GetComponentsInChildren<Transform>(true)
				where names.Contains(t.name)
				select t).FirstOrDefault();
			if (transform == null)
			{
				return null;
			}
			return transform.Find("attach");
		}
	}
}
