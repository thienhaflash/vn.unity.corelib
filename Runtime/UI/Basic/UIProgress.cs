using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace vn.corelib
{
	public class UIProgress : MonoBehaviour
	{
		[Flags] enum ProgressBinding
		{
			ImageFill = 1,
			Alpha = 2,
			Text = 4,
			AnchoredPosition = 8,
			AnchoredSize = 16
		}
		
		class BaseBinding
		{
			public virtual void Refresh(float p)
			{
				
			}
		}

		[Serializable] class ImageFillBinding : BaseBinding
		{
			public Image target;
			
			public override void Refresh(float p)
			{
				target.fillAmount = p;
			}
		}
		[Serializable] class AlphaBinding : BaseBinding
		{
			public Graphic target;
			
			public override void Refresh(float p)
			{
				Color c = target.color;
				c.a = p;
				target.color = c;
			}
		}
		
		[Serializable] class TextBinding : BaseBinding
		{
			public Component target;
			public string format = "{0:#,###.##}";
			public float min = 0;
			public float max = 100;
			
			[NonSerialized] private PropertyInfo _property;
			[NonSerialized] private bool _tried = false;
			
			public override void Refresh(float p)
			{
				if (target == null) return;
				var p2 = (p * (max - min) + min);
				if (_property == null)
				{
					if (_tried) return;
					_tried = true;
					_property = target.GetType().GetProperty("text");
				}
				
				var text = string.Format(format, p2);
				_property?.SetValue(target, text);
			}
		}
		[Serializable] class AnchoredPositionBinding : BaseBinding
		{
			public RectTransform target;
			public bool isHorz;
			public Vector2 padding = new Vector2(0.5f, 0.5f);
			
			public override void Refresh(float p)
			{
				if (target == null) return;
				
				var parent = (RectTransform)target.parent;
				if (parent == null) return; // in prefab?
				
				Rect bounds = parent.rect;
				Rect targetRect = target.rect;

				var (w, h) = (targetRect.width, targetRect.height);
				var (px, py) = (padding.x, padding.y);
				
				Vector2 pos = target.anchoredPosition;

				if (isHorz)
				{
					var w2 = bounds.width - (px + py) * w;
					pos.x = - target.anchorMin.x * bounds.width + p * w2 + px * w;
				}
				else
				{
					var h2 = bounds.height - (px + py) * h;
					pos.y = (p-target.anchorMin.y) * h2 + px * h;
				}
				
				target.anchoredPosition = pos;
			}
		}
		[Serializable] class AnchoredSizeBinding : BaseBinding
		{
			public RectTransform target;
			public bool isHorz;
			
			public override void Refresh(float p)
			{
				Vector2 max = target.anchorMax;
				if (isHorz)
				{
					max.x = p;
				}
				else
				{
					max.y = p;
				}
				
				target.anchorMax = max;
			}
		}
		
		[SerializeField] ProgressBinding mode;
		
		[FormerlySerializedAs("image")] 
		[SerializeField] ImageFillBinding imageFill;
		
		[SerializeField] AlphaBinding alpha;
		[SerializeField] TextBinding text;
		[SerializeField] AnchoredPositionBinding anchoredPos;
		[SerializeField] AnchoredSizeBinding anchorSize;
		
		[Range(0f, 1f)] public float progress;
		[NonSerialized] private readonly List<BaseBinding> list = new List<BaseBinding>();

		void Awake()
		{
			Preprocess();
		}

		#if UNITY_EDITOR
		private void OnValidate()
		{
			if (Application.isPlaying) return;
			if (string.IsNullOrEmpty(gameObject.scene.name)) return; // prefabMode

			EditorApplication.update -= DelayRefresh;
			EditorApplication.update += DelayRefresh;
		}
		
		void DelayRefresh()
		{
			EditorApplication.update -= DelayRefresh;
			if (this == null || gameObject == null) return;
			
			Preprocess();
			Refresh();
		}
		
		#endif

		[Button] void Preprocess()
		{
			list.Clear();
			if ((mode & ProgressBinding.ImageFill) != 0) list.Add(imageFill);
			if ((mode & ProgressBinding.Alpha) != 0) list.Add(alpha);
			if ((mode & ProgressBinding.Text) != 0) list.Add(text);
			if ((mode & ProgressBinding.AnchoredPosition) != 0) list.Add(anchoredPos);
			if ((mode & ProgressBinding.AnchoredSize) != 0) list.Add(anchorSize);
		}

		public void SetProgress(float v)
		{
			v = Mathf.Clamp01(v);
			if (Math.Abs(progress - v) < 0.001f) return;
			
			progress = v;
			Refresh();
		}
		
		[ContextMenu("Refresh")]
		public void Refresh()
		{
			for (var i = 0; i < list.Count; i++)
			{
				list[i].Refresh(progress);
			}
		}
	}	
}

