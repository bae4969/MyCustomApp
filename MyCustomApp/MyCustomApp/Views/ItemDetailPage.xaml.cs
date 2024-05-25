using MyCustomApp.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace MyCustomApp.Views
{
	public partial class ItemDetailPage : ContentPage
	{
		public ItemDetailPage()
		{
			InitializeComponent();
			BindingContext = new ItemDetailViewModel();
		}
	}
}