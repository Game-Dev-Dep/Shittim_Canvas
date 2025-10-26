using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIClickEffects_Services : MonoBehaviour
{
	[SerializeField] private Canvas targetCanvas;
	[SerializeField] private Sprite ringSprite;
	[SerializeField] private Color ringColor = new Color(1f, 1f, 1f, 0.6f);
	[SerializeField] private Color particleColor = new Color(1f, 1f, 1f, 0.9f);
	[SerializeField] private int particleCount = 12;
	[SerializeField] private float ringDuration = 0.35f;
	[SerializeField] private float particleDuration = 0.35f;
	[SerializeField] private float ringStartSize = 10f;
	[SerializeField] private float ringEndSize = 160f;
	[SerializeField] private Vector2 particleSize = new Vector2(8f, 8f);
	[SerializeField] private Vector2 particleDistanceRange = new Vector2(60f, 120f);

	[SerializeField] private bool enableTrail = true;
	[SerializeField] private float trailDistanceStep = 20f;
	[SerializeField] private Vector2 trailSize = new Vector2(7f, 7f);
	[SerializeField] private float trailDuration = 0.25f;
	[SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.7f);

	[SerializeField] private bool enableGlow = true;
	[SerializeField] private float particleGlowScale = 1.8f;
	[SerializeField] private float particleGlowAlphaMultiplier = 0.35f;
	[SerializeField] private float trailGlowWidthMultiplier = 2.2f;
	[SerializeField] private float trailGlowAlphaMultiplier = 0.35f;

	// 优化用参数
	[SerializeField] private int maxTrailSegmentsPerFrame = 5;
	[SerializeField] private float maxTrailDistance = 200f;

	private RectTransform container;
	private bool hasLastTrailPos;
	private Vector2 lastTrailLocalPos;

	void Awake()
	{
		if (targetCanvas == null)
			targetCanvas = FindObjectOfType<Canvas>();

		if (targetCanvas == null)
		{
			Debug.LogWarning("UIClickEffects: 未找到 Canvas，功能已禁用。");
			enabled = false;
			return;
		}

		if (ringSprite == null)
			ringSprite = Resources.Load<Sprite>("Sprites/Circle");

		var containerGo = new GameObject("UIClickEffects_Container", typeof(RectTransform));
		container = containerGo.GetComponent<RectTransform>();
		container.SetParent(targetCanvas.transform, false);
		container.anchorMin = Vector2.zero;
		container.anchorMax = Vector2.one;
		container.offsetMin = Vector2.zero;
		container.offsetMax = Vector2.zero;
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (TryGetMousePosition(out Vector2 localPoint))
			{
				SpawnRing(localPoint);
				SpawnParticles(localPoint);
				hasLastTrailPos = true;
				lastTrailLocalPos = localPoint;
			}
		}

		if (enableTrail && Input.GetMouseButton(0))
		{
			if (TryGetMousePosition(out Vector2 localPoint))
			{
				if (!hasLastTrailPos)
				{
					hasLastTrailPos = true;
					lastTrailLocalPos = localPoint;
				}

				float dist = Vector2.Distance(lastTrailLocalPos, localPoint);
				if (dist >= trailDistanceStep)
				{
					// 限制最大拖尾距离
					if (dist > maxTrailDistance)
					{
						lastTrailLocalPos = localPoint;
						return;
					}

					int steps = Mathf.FloorToInt(dist / trailDistanceStep);
					steps = Mathf.Min(steps, maxTrailSegmentsPerFrame);
			
					Vector2 prev = lastTrailLocalPos;
					for (int i = 1; i <= steps; i++)
					{
						Vector2 p = Vector2.Lerp(lastTrailLocalPos, localPoint, (float)i / steps);
						SpawnTrailSegment(prev, p);
						prev = p;
					}
					lastTrailLocalPos = localPoint;
				}
			}
		}

		if (Input.GetMouseButtonUp(0))
			hasLastTrailPos = false;
	}

	private bool TryGetMousePosition(out Vector2 localPoint)
	{
		return RectTransformUtility.ScreenPointToLocalPointInRectangle(
			targetCanvas.transform as RectTransform,
			Input.mousePosition,
			GetCanvasCamera(targetCanvas),
			out localPoint);
	}

	private Camera GetCanvasCamera(Canvas canvas)
	{
		switch (canvas.renderMode)
		{
			case RenderMode.ScreenSpaceCamera:
			case RenderMode.WorldSpace:
				return canvas.worldCamera;
			default:
				return null;
		}
	}

	private void SpawnRing(Vector2 localPos)
	{
		var go = new GameObject("ClickRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		var rt = go.GetComponent<RectTransform>();
		var img = go.GetComponent<Image>();
		img.raycastTarget = false;
		img.sprite = ringSprite;
		img.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0f);

		rt.SetParent(container, false);
		rt.anchoredPosition = localPos;
		rt.sizeDelta = new Vector2(ringStartSize, ringStartSize);

		StartCoroutine(AnimateRing(rt, img));
	}

	private void SpawnParticles(Vector2 localPos)
	{
		for (int i = 0; i < particleCount; i++)
		{
			CreateParticle(localPos);
		}
	}

	private void CreateParticle(Vector2 localPos)
	{
		var go = new GameObject("ClickParticle_Triangle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TriangleGraphic));
		var rt = go.GetComponent<RectTransform>();
		var tri = go.GetComponent<TriangleGraphic>();
		tri.raycastTarget = false;
		float aFactor = Random.Range(0.5f, 1f);
		Color baseCol = particleColor; baseCol.a *= aFactor;
		tri.color = baseCol;

		rt.SetParent(container, false);
		rt.anchoredPosition = localPos;
		rt.sizeDelta = particleSize;
		rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

		Vector2 dir = Random.insideUnitCircle.normalized;
		float dist = Random.Range(particleDistanceRange.x, particleDistanceRange.y);
		Vector2 end = localPos + dir * dist;

		StartCoroutine(AnimateElement(rt, tri, localPos, end, particleDuration, baseCol));

		if (enableGlow)
		{
			var glowGo = new GameObject("ClickParticle_Triangle_Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TriangleGraphic));
			var glowRt = glowGo.GetComponent<RectTransform>();
			var glowTri = glowGo.GetComponent<TriangleGraphic>();
			glowTri.raycastTarget = false;
			Color glowCol = baseCol; glowCol.a *= particleGlowAlphaMultiplier;
			glowTri.color = glowCol;
			glowRt.SetParent(container, false);
			glowRt.anchoredPosition = localPos;
			glowRt.sizeDelta = particleSize * particleGlowScale;
			glowRt.localEulerAngles = rt.localEulerAngles;
			StartCoroutine(AnimateElement(glowRt, glowTri, localPos, end, particleDuration, glowCol));
		}
	}

	private void SpawnTrailSegment(Vector2 start, Vector2 end)
	{
		float len = Vector2.Distance(start, end);
		if (len <= 0.001f) return;

		var go = new GameObject("DragTrail_Segment", typeof(RectTransform), typeof(CanvasRenderer), typeof(TaperedSegmentGraphic));
		var rt = go.GetComponent<RectTransform>();
		var sg = go.GetComponent<TaperedSegmentGraphic>();
		sg.raycastTarget = false;
		float aFactor = Random.Range(0.35f, 1f);
		Color baseCol = trailColor; baseCol.a *= aFactor;
		sg.color = baseCol;

		rt.SetParent(container, false);
		rt.pivot = new Vector2(0.5f, 0.5f);
		rt.anchoredPosition = (start + end) * 0.5f;
		rt.sizeDelta = new Vector2(len, trailSize.y);
		Vector2 dir = (end - start).normalized;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		rt.localEulerAngles = new Vector3(0f, 0f, angle);

		StartCoroutine(AnimateElement(rt, sg, rt.anchoredPosition, rt.anchoredPosition, trailDuration, baseCol));

		if (enableGlow)
		{
			var glowGo = new GameObject("DragTrail_Segment_Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TaperedSegmentGraphic));
			var glowRt = glowGo.GetComponent<RectTransform>();
			var glowG = glowGo.GetComponent<TaperedSegmentGraphic>();
			glowG.raycastTarget = false;
			Color glowCol = baseCol; glowCol.a *= trailGlowAlphaMultiplier;
			glowG.color = glowCol;
			glowRt.SetParent(container, false);
			glowRt.pivot = new Vector2(0.5f, 0.5f);
			glowRt.anchoredPosition = rt.anchoredPosition;
			glowRt.sizeDelta = new Vector2(len, trailSize.y * trailGlowWidthMultiplier);
			glowRt.localEulerAngles = rt.localEulerAngles;
			StartCoroutine(AnimateElement(glowRt, glowG, glowRt.anchoredPosition, glowRt.anchoredPosition, trailDuration, glowCol));
		}
	}

	private IEnumerator AnimateRing(RectTransform rt, Image img)
	{
		float t = 0f;
		while (t < ringDuration)
		{
			t += Time.unscaledDeltaTime;
			float p = Mathf.Clamp01(t / ringDuration);
			float size = Mathf.Lerp(ringStartSize, ringEndSize, EaseOutCubic(p));
			rt.sizeDelta = new Vector2(size, size);
			Color c = ringColor;
			c.a *= (1f - p);
			img.color = c;
			yield return null;
		}
		Destroy(rt.gameObject);
	}

	private IEnumerator AnimateElement(RectTransform rt, Graphic graphic, Vector2 start, Vector2 end, float duration, Color baseColor)
	{
		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			float p = Mathf.Clamp01(t / duration);
			rt.anchoredPosition = Vector2.Lerp(start, end, EaseOutCubic(p));
			float scale = Mathf.Lerp(1f, 0.6f, p);
			rt.localScale = new Vector3(scale, scale, 1f);
			Color c = baseColor;
			c.a *= (1f - p);
			graphic.color = c;
			yield return null;
		}
		Destroy(rt.gameObject);
	}

	private float EaseOutCubic(float x)
	{
		x = Mathf.Clamp01(x);
		return 1f - Mathf.Pow(1f - x, 3f);
	}

	private class TriangleGraphic : MaskableGraphic
	{
		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			var r = GetPixelAdjustedRect();
			float w = r.width;
			float h = r.height;
			Vector2 p0 = new Vector2(r.x + w * 0.5f, r.y + h);
			Vector2 p1 = new Vector2(r.x, r.y);
			Vector2 p2 = new Vector2(r.x + w, r.y);

			var color32 = color;
			vh.AddVert(p0, color32, new Vector2(0.5f, 1f));
			vh.AddVert(p1, color32, new Vector2(0f, 0f));
			vh.AddVert(p2, color32, new Vector2(1f, 0f));
			vh.AddTriangle(0, 1, 2);
		}
	}

	private class TaperedSegmentGraphic : MaskableGraphic
	{
		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			var r = GetPixelAdjustedRect();
			float h = r.height;
			float taper = h * 0.5f;

			Vector2 bl = new Vector2(r.xMin, r.yMin);
			Vector2 tl = new Vector2(r.xMin, r.yMax);
			Vector2 tr = new Vector2(r.xMax, r.yMax);
			Vector2 br = new Vector2(r.xMax, r.yMin);

			Vector2 tlp = new Vector2(r.xMin + taper, r.yMax);
			Vector2 trp = new Vector2(r.xMax - taper, r.yMax);
			Vector2 blp = new Vector2(r.xMin + taper, r.yMin);
			Vector2 brp = new Vector2(r.xMax - taper, r.yMin);

			var c = color;
			vh.AddVert(bl, c, Vector2.zero);
			vh.AddVert(tl, c, Vector2.zero);
			vh.AddVert(tr, c, Vector2.zero);
			vh.AddVert(br, c, Vector2.zero);
			vh.AddVert(tlp, c, Vector2.zero);
			vh.AddVert(trp, c, Vector2.zero);
			vh.AddVert(blp, c, Vector2.zero);
			vh.AddVert(brp, c, Vector2.zero);

			vh.AddTriangle(0, 1, 4);
			vh.AddTriangle(4, 1, 5);
			vh.AddTriangle(5, 2, 6);
			vh.AddTriangle(6, 2, 3);
			vh.AddTriangle(4, 5, 6);
			vh.AddTriangle(6, 5, 7);
		}
	}
}