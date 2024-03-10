using System;
using System.Collections.Generic;
using UnityEngine;

namespace Piot.UnityNetworkTools
{
	public class Lagometer : MonoBehaviour
	{
		[SerializeField] GUIStyle guiStyle;

		IEnumerable<int> snapshotLatencies = ArraySegment<int>.Empty;
		Texture? backgroundTexture;

		Texture? dropTexture;
		Texture? receivedTexture;

		public Vector2 topLeft;
		public int height;
		public int scaleX;

		void Awake()
		{
			backgroundTexture = CreateTextureForColor(new Color(0, 0, 0, 0.9f));
			const float alpha = 0.5f;
			dropTexture = CreateTextureForColor(new Color(1.0f, 0, 0, alpha));
			receivedTexture = CreateTextureForColor(new Color(0, 1.0f, 0, alpha));
		}

		/*
		void FixedUpdate()
		{
		    var y = 200;

		    debugLastInterpolationValue += 25;
		    debugInterpolationCount++;
		    if (debugInterpolationCount >= debugNextInterpolationCountThreshold)
		    {
		        debugInterpolationCount = 0;
		        debugNextInterpolationCountThreshold = UnityEngine.Random.Range(2, 7);
		        debugLastInterpolationValue = -100;
		    }

		    interpolationValues.Enqueue(debugLastInterpolationValue);
		    if (interpolationValues.Count > 200)
		    {
		        interpolationValues.Dequeue();
		    }

		    if (UnityEngine.Random.Range(0, 100) < 14)
		    {
		        return;
		    }

		    var value = 16 + UnityEngine.Random.Range(0, 50);
		    if (UnityEngine.Random.Range(0, 100) < 8)
		    {
		        value = -value;
		    }

		    snapshotLatencies.Enqueue(value);
		    if (snapshotLatencies.Count > 200)
		    {
		        snapshotLatencies.Dequeue();
		    }
		}
		*/
		public IEnumerable<int> SnapshotLatencies
		{
			set => snapshotLatencies = value;
		}

		void OnGUI()
		{
			var basePos = topLeft;
			GUI.DrawTexture(new Rect(basePos.x, basePos.y - height, 120 * scaleX, height), backgroundTexture,
				ScaleMode.StretchToFill, true, 0);

			var i = 0;
			foreach (var value in snapshotLatencies)
			{
				if(value < 0)
				{
					DrawBar(i, 80, dropTexture);
				}
				else
				{
					DrawBar(i, value, receivedTexture);
				}

				i += scaleX - 1;
			}


			GUI.Label(new Rect(basePos.x, basePos.y, 120 * scaleX, height), "Lagometer", guiStyle);
		}

		static Texture CreateTextureForColor(Color color)
		{
			var colorTexture = new Texture2D(1, 1);
			colorTexture.SetPixel(0, 0, color);
			colorTexture.wrapMode = TextureWrapMode.Repeat;
			colorTexture.Apply();
			return colorTexture;
		}

		void DrawBar(int index, int value, Texture texture)
		{
			var scaledValue = value * height / 500;
			var basePos = topLeft;
			var xOffset = index * 2;
			GUI.DrawTexture(new Rect(basePos.x + xOffset, basePos.y - scaledValue, scaleX, scaledValue), texture,
				ScaleMode.StretchToFill, true, 0);
		}
	}
}