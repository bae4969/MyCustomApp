using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MyCustomApp.Pages
{
	public partial class SpeedmeterPage : ContentPage
	{
		private bool isListening;

		public SpeedmeterPage()
		{
			InitializeComponent();
			RequestLocationPermission();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			StartListening();
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			StopListening();
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

		private void StartListening()
		{
			isListening = true;
			Device.StartTimer(TimeSpan.FromSeconds(1), () =>
			{
				UpdateLocation();
				return isListening; // true를 반환하면 타이머가 계속 작동합니다.
			});
		}

		private void StopListening()
		{
			isListening = false;
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
						SpeedLabel.Text = $"{speed:F1} km/h";
					});
				}
			}
			catch (Exception ex)
			{
				// 예외 처리
				Console.WriteLine($"Unable to get location: {ex.Message}");
			}
		}
	}
}
