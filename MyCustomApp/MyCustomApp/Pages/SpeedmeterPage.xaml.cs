using MyCustomApp.Services;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyCustomApp.Pages
{
	public partial class SpeedmeterPage : ContentPage
	{
		const double MaxAcc = 0.5;
		const double MaxOffset = 130; // 상단 버블의 최대 이동 거리

		private bool isListening = false;
		private bool isNeedUpdateBase = false;
		private Vector3 baseAccVal = new Vector3(0.0f, 9.81f, 0.0f);

		readonly List<double> AccXQueue = new List<double>();
		readonly List<double> AccYQueue = new List<double>();

		public SpeedmeterPage()
		{
			InitializeComponent();
			RequestLocationPermission();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			isListening = true;
			isNeedUpdateBase = true;
			Device.StartTimer(TimeSpan.FromSeconds(1), () =>
			{
				UpdateLocation();
				return isListening; // true를 반환하면 타이머가 계속 작동합니다.
			});
			DeviceDisplay.KeepScreenOn = true;
			Accelerometer.ReadingChanged += OnAccelerometerReadingChanged;
			Accelerometer.Start(SensorSpeed.UI);
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			isListening = false;
			DeviceDisplay.KeepScreenOn = false;
			Accelerometer.ReadingChanged -= OnAccelerometerReadingChanged;
			Accelerometer.Stop();
		}

		private void OnSetZeroAccClicked(object sender, EventArgs e)
		{
			isNeedUpdateBase = true;
		}

		private async void RequestLocationPermission()
		{
			var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
			if (status != PermissionStatus.Granted)
			{
				status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
			}

			if (status != PermissionStatus.Granted)
			{
				await DisplayAlert("권한 필요", "앱이 정상적으로 동작하려면 위치 권한이 필요합니다.", "확인");
			}
		}

		private async void UpdateLocation()
		{
			try
			{
				var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1));
				var location = await Geolocation.GetLocationAsync(request);

				if (location != null)
				{
					int speed = location.Speed.HasValue ? (int)(location.Speed.Value * 3.6) : 0; // 속도를 km/h로 변환
					Device.BeginInvokeOnMainThread(() =>
					{
						SpeedLabel.Text = $"{speed} km/h";
					});
				}
			}
			catch (Exception ex)
			{
				// 예외 처리
				Console.WriteLine($"Unable to get location: {ex.Message}");
			}
		}

		void OnAccelerometerReadingChanged(object sender, AccelerometerChangedEventArgs e)
		{
			var data = e.Reading;

			if (isNeedUpdateBase)
			{
				isNeedUpdateBase = false;
				baseAccVal = data.Acceleration;
			}

			Vector3 t_acc = data.Acceleration - baseAccVal;
			double acc_x = t_acc.X;
			double acc_y = t_acc.Z;

			AccXQueue.Add(acc_x);
			AccYQueue.Add(acc_y);

			while (AccXQueue.Count > 3) AccXQueue.RemoveAt(0);
			while (AccYQueue.Count > 3) AccYQueue.RemoveAt(0);

			double acc_x_avg = 0.0;
			double acc_y_avg = 0.0;
			foreach (double val in AccXQueue) acc_x_avg += val;
			foreach (double val in AccYQueue) acc_y_avg += val;
			acc_x_avg /= AccXQueue.Count;
			acc_y_avg /= AccYQueue.Count;

			double acc_tot = Math.Sqrt(acc_x_avg * acc_x_avg + acc_y_avg * acc_y_avg);
			double center_multiplier =
				acc_tot > MaxAcc ?
				1.0 / acc_tot :
				1.0 / MaxAcc;

			Device.BeginInvokeOnMainThread(() =>
			{
				CentralBubble.TranslationX = MaxOffset * center_multiplier * acc_x_avg;
				CentralBubble.TranslationY = MaxOffset * center_multiplier * acc_y_avg;

				// 각도 값 업데이트
				AccT.Text = $"T: {acc_tot:F2}";
				AccX.Text = $"X: {Math.Abs(acc_x_avg):F2}";
				AccY.Text = $"X: {Math.Abs(acc_y_avg):F2}";
			});
		}
	}
}
