/*
using System.Collections.Generic;
using UnityEngine;

namespace Piot.UnityNetworkTools
{
	public class InterpolationGraph : MonoBehaviour
	{
		readonly Queue<int> interpolationValues = new();
		int debugLastInterpolationValue = -20;
		int debugNextInterpolationCountThreshold = 3;
		Texture? extrapolationTexture;
		Texture? interpolationTexture;
		int debugInterpolationCount;

		void Awake()
		{
			interpolationTexture = CreateTextureForColor(new Color(0, 0, 1.0f, alpha));
			extrapolationTexture = CreateTextureForColor(new Color(1.0f, 1.0f, 0, alpha));
		}

		void OnGui()
		{
			i = 0;
			var lineY = -100;
			foreach (var value in interpolationValues)
			{
				DrawLine(i, lineY, value, value < 0 ? interpolationTexture : extrapolationTexture);

				++i;
			}
		}

		static void DrawLine(int index, int y, int value, Texture texture)
		{
			var basePos = new Vector2(300, 300);
			var xOffset = index * 2;
			var adjustedValue = value / 10;
			var pixelValue = adjustedValue == 0 ? 1 : adjustedValue;
			GUI.DrawTexture(new Rect(basePos.x + xOffset, basePos.y + y - adjustedValue, 3, pixelValue), texture,
				ScaleMode.StretchToFill, true, 0);
		}
	}
}
*/