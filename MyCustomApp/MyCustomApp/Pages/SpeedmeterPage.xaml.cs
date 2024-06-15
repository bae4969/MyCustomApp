using MyCustomApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyCustomApp.Pages
{
	public partial class SpeedmeterPage : ContentPage
	{
		private bool isListening = false;
		private readonly int SAMPLING_INTERVAL = 500;
		private readonly int SPEED_SAMPLE_RANGE = 1000;
		private readonly int INCLINE_SAMPLE_RANGE = 30000;
		private readonly int SPEED_SMAPLE_CNT;
		private readonly int INCLINE_SMAPLE_CNT;
		private readonly int MAX_SMAPLE_CNT;
		private readonly Queue<Location> lastLocations = new Queue<Location>();

		private int outputSpeed = 0;
		private double outputIncline = 0.0;

		public SpeedmeterPage()
		{
			SPEED_SMAPLE_CNT = SPEED_SAMPLE_RANGE / SAMPLING_INTERVAL;
			INCLINE_SMAPLE_CNT = INCLINE_SAMPLE_RANGE / SAMPLING_INTERVAL;
			MAX_SMAPLE_CNT = Math.Max(SPEED_SMAPLE_CNT, INCLINE_SMAPLE_CNT);
			InitializeComponent();
			RequestLocationPermission();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			isListening = true;
			Device.StartTimer(TimeSpan.FromMilliseconds(SAMPLING_INTERVAL), () =>
			{
				UpdateLocation();
				return isListening; // true를 반환하면 타이머가 계속 작동합니다.
			});
			DeviceDisplay.KeepScreenOn = true;
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			isListening = false;
			DeviceDisplay.KeepScreenOn = false;
			Accelerometer.Stop();
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
				var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromMilliseconds(SAMPLING_INTERVAL));
				var location = await Geolocation.GetLocationAsync(request);
				if (location == null) return;


				lastLocations.Enqueue(location);
				while (lastLocations.Count > MAX_SMAPLE_CNT)
					lastLocations.Dequeue();
				int dataCnt = lastLocations.Count;


				if (dataCnt >= SPEED_SMAPLE_CNT)
				{
					double tot_speed = 0.0;
					double tot_cnt = 0.0;
					for (int idx = Math.Max(dataCnt - SPEED_SMAPLE_CNT, 0); idx < dataCnt; idx++)
					{
						var loc = lastLocations.ElementAt(idx);
						if (loc.Speed == null) continue;

						tot_speed += (double)(loc.Speed.Value * 3.6);
						tot_cnt += 1.0;
					}
					if (tot_cnt > 0)
						outputSpeed = (int)Math.Round(tot_speed / tot_cnt);
				}


				if (dataCnt >= INCLINE_SMAPLE_CNT)
				{
					double tot_distance = 0.0;
					double tot_elevation = 0.0;

					for (int idx = Math.Max(dataCnt - INCLINE_SMAPLE_CNT, 0); idx + 1 < dataCnt; idx++)
					{
						var loc_from = lastLocations.ElementAt(idx);
						var loc_to = lastLocations.ElementAt(idx + 1);
						if (loc_from.Altitude == null || loc_to.Altitude == null) continue;

						tot_distance += Location.CalculateDistance(loc_from, loc_to, DistanceUnits.Kilometers) * 1000.0;
						tot_elevation += (double)loc_to.Altitude - (double)loc_from.Altitude;
					}
					if (tot_distance > 100)
						outputIncline = tot_elevation / tot_distance * 100.0;
				}


				Device.BeginInvokeOnMainThread(() =>
				{
					SpeedLabel.Text = $"{outputSpeed} km/h";
					InclineLabel.Text = $"{outputIncline:F1} %";
				});
			}
			catch (Exception ex)
			{
				// 예외 처리
				Console.WriteLine($"Unable to get location: {ex.Message}");
			}
		}
	}
}
