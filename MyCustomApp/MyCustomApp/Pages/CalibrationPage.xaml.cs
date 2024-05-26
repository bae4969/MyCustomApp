using System;
using System.Collections;
using System.Collections.Generic;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyCustomApp.Pages
{
	public partial class CalibrationPage : ContentPage
	{
		const double MaxAngle = 45.0;
		const double MaxOffset = 130; // 상단 버블의 최대 이동 거리

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

			double limited_angle_x = angleX_avg;
			double limited_angle_y = angleY_avg;
			if (limited_angle_x > 0 && limited_angle_x > MaxAngle) limited_angle_x = MaxAngle;
			if (limited_angle_y > 0 && limited_angle_y > MaxAngle) limited_angle_y = MaxAngle;
			else if(limited_angle_x < 0 && limited_angle_x < -MaxAngle) limited_angle_x = -MaxAngle;
			else if (limited_angle_y < 0 && limited_angle_y < -MaxAngle) limited_angle_y = -MaxAngle;

			// 상단 버블과 오른쪽 버블의 위치 제한
			double multiX = limited_angle_x / MaxAngle;
			double multiY = limited_angle_y / MaxAngle;

			double angleTot = Math.Sqrt(multiX * multiX + multiY * multiY);
			double multiC = angleTot > 1.0 ? 1.0 / angleTot : 1.0;
			//double multiC = angleTot > MaxAngle ? 1.0 : angleTot / MaxAngle;

			Device.BeginInvokeOnMainThread(() =>
			{
				HorizontalBubble.TranslationX = MaxOffset * multiX;
				VerticalBubble.TranslationY = -MaxOffset * multiY;

				// 중앙 버블의 위치 업데이트 (좌우 및 상하 각도)
				CentralBubble.TranslationX = MaxOffset * multiX * multiC;
				CentralBubble.TranslationY = -MaxOffset * multiY * multiC;

				// 각도 값 업데이트
				TiltLabelX.Text = $"X: {angleX_avg:F1}";
				TiltLabelY.Text = $"Y: {angleY_avg:F1}";

				// 색상 변경 로직
				HorizontalBubble.Color = Math.Abs(angleX_avg) <= 2 ? Color.FromRgb(200, 50, 50) : Color.FromRgb(50, 50, 200);
				VerticalBubble.Color = Math.Abs(angleY_avg) <= 2 ? Color.FromRgb(200, 50, 50) : Color.FromRgb(50, 50, 200);
				CentralBubble.Color = Math.Abs(angleX_avg) <= 2 && Math.Abs(angleY_avg) <= 2 ? Color.FromRgb(200, 50, 50) : Color.FromRgb(50, 50, 200);
			});
		}

		private double AdjustAngle(double angle)
		{
			if (angle > 90)
				angle = 180 - angle;
			if (angle < -90)
				angle = -180 - angle;

			return angle;
		}
	}
}
