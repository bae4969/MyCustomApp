using System;
using System.Collections;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyCustomApp.Pages
{
	public partial class CalibrationPage : ContentPage
	{
		const double MaxBubbleOffset = 80; // 외부 원의 반지름 - 중앙 버블의 반지름
		const double MaxHorizontalOffset = 80; // 상단 버블의 최대 이동 거리
		const double MaxVerticalOffset = 80; // 오른쪽 버블의 최대 이동 거리

		List<double> AngleXQueue = new List<double>();
		List<double> AngleYQueue = new List<double>();

		public CalibrationPage()
		{
			InitializeComponent();
			DeviceDisplay.KeepScreenOn = true;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			Accelerometer.ReadingChanged += OnAccelerometerReadingChanged;
			Accelerometer.Start(SensorSpeed.UI);
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			Accelerometer.ReadingChanged -= OnAccelerometerReadingChanged;
			Accelerometer.Stop();
		}

		void OnAccelerometerReadingChanged(object sender, AccelerometerChangedEventArgs e)
		{
			var data = e.Reading;

			// X축과 Y축의 각도 계산 (90도에서 -90도 범위로 제한)
			double angleX = Math.Atan2(data.Acceleration.X, data.Acceleration.Z) * (180 / Math.PI);
			double angleY = Math.Atan2(data.Acceleration.Y, data.Acceleration.Z) * (180 / Math.PI);

			angleX = AdjustAngle(angleX);
			angleY = AdjustAngle(angleY);

			AngleXQueue.Add(angleX);
			AngleYQueue.Add(angleY);

			while (AngleXQueue.Count > 10) AngleXQueue.RemoveAt(0);
			while (AngleYQueue.Count > 10) AngleYQueue.RemoveAt(0);

			double angleX_avg = 0.0;
			double angleY_avg = 0.0;
			foreach (double val in AngleXQueue) angleX_avg += val;
			foreach (double val in AngleYQueue) angleY_avg += val;
			angleX_avg /= AngleXQueue.Count;
			angleY_avg /= AngleYQueue.Count;

			// 중앙 버블의 이동 거리를 계산
			double bubbleX = angleX;
			double bubbleY = angleY;

			bubbleX = AdjustAngle(bubbleX);
			bubbleY = AdjustAngle(bubbleY);

			// 상단 버블과 오른쪽 버블의 위치 제한
			double limitedHorizontalX = MathExtensions.Clamp(angleX, -MaxHorizontalOffset, MaxHorizontalOffset);
			double limitedVerticalY = MathExtensions.Clamp(angleY, -MaxVerticalOffset, MaxVerticalOffset);

			// 중앙 버블이 외부 원 밖으로 나가지 않도록 제한
			double distance = Math.Sqrt(bubbleX * bubbleX + bubbleY * bubbleY);
			if (distance > MaxBubbleOffset)
			{
				double scale = MaxBubbleOffset / distance;
				bubbleX *= scale;
				bubbleY *= scale;
			}

			Device.BeginInvokeOnMainThread(() =>
			{
				// 상단 버블의 위치 업데이트 (좌우 각도)
				HorizontalBubble.TranslationX = limitedHorizontalX;

				// 오른쪽 버블의 위치 업데이트 (상하 각도)
				VerticalBubble.TranslationY = -limitedVerticalY;

				// 중앙 버블의 위치 업데이트 (좌우 및 상하 각도)
				CentralBubble.TranslationX = bubbleX;
				CentralBubble.TranslationY = -bubbleY;

				// 각도 값 업데이트
				TiltLabelX.Text = $"X: {angleX:F1}";
				TiltLabelY.Text = $"Y: {angleY:F1}";

				// 색상 변경 로직
				HorizontalBubble.Color = Math.Abs(angleX) <= 2 ? Color.Red : Color.Blue;
				VerticalBubble.Color = Math.Abs(angleY) <= 2 ? Color.Red : Color.Blue;

				// 중앙 버블 색상 변경 (상단과 오른쪽이 동시에 -2~2도 사이일 때)
				if (Math.Abs(angleX) <= 2 && Math.Abs(angleY) <= 2)
				{
					CentralBubble.Color = Color.Red;
				}
				else
				{
					CentralBubble.Color = Color.Blue;
				}
			});
		}

		private double AdjustAngle(double angle)
		{
			if (angle > 90)
				angle = 180 - angle;
			if (angle < -90)
				angle = -180 - angle;
			return MathExtensions.Clamp(angle, -90, 90);
		}
	}

	public static class MathExtensions
	{
		public static double Clamp(double value, double min, double max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}
	}
}
