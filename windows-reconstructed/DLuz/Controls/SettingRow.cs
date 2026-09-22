using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace DLuz.Controls;

public partial class SettingRow : UserControl, IComponentConnector
{
	public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(SettingRow), new PropertyMetadata(""));

	public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(SettingRow), new PropertyMetadata(""));

	public static readonly DependencyProperty ActionProperty = DependencyProperty.Register("Action", typeof(object), typeof(SettingRow), new PropertyMetadata(null));

	public static readonly DependencyProperty IsSubOptionProperty = DependencyProperty.Register("IsSubOption", typeof(bool), typeof(SettingRow), new PropertyMetadata(false));

	public string Title
	{
		get
		{
			return (string)GetValue(TitleProperty);
		}
		set
		{
			SetValue(TitleProperty, value);
		}
	}

	public string Description
	{
		get
		{
			return (string)GetValue(DescriptionProperty);
		}
		set
		{
			SetValue(DescriptionProperty, value);
		}
	}

	public object? Action
	{
		get
		{
			return GetValue(ActionProperty);
		}
		set
		{
			SetValue(ActionProperty, value);
		}
	}

	public bool IsSubOption
	{
		get
		{
			return (bool)GetValue(IsSubOptionProperty);
		}
		set
		{
			SetValue(IsSubOptionProperty, value);
		}
	}

	public SettingRow()
	{
		InitializeComponent();
	}
}

