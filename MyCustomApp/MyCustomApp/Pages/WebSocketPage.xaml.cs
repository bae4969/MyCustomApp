using System;
using WebSocket4Net;
using Xamarin.Forms;

namespace MyCustomApp.Pages
{
	public partial class WebSocketPage : ContentPage
	{
		WebSocket ws;

		public WebSocketPage()
		{
			InitializeComponent();
		}

		private void OnConnectButtonClicked(object sender, EventArgs e)
		{
			ws = new WebSocket("ws://135.135.135.30:49695");
			ws.Opened += (s, args) =>
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					ResponseLabel.Text = "Connected to server.";
				});
			};
			ws.MessageReceived += (s, args) =>
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					ResponseLabel.Text = "Server says: " + args.Message;
				});
			};
			ws.Open();
		}

		private void OnSendMessageButtonClicked(object sender, EventArgs e)
		{
			if (ws != null && ws.State == WebSocketState.Open)
			{
				ws.Send(MessageEntry.Text);
			}
		}
	}
}
