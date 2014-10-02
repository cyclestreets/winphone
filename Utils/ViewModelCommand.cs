using System;
using System.Windows.Input;

namespace Cyclestreets.Utils
{
	/// <summary>
	/// Defines a command that can be bound to from XAML and redirects to a handler function.
	/// </summary>
	public class ViewModelCommand : ICommand
	{
		private readonly Action _handler;


		public ViewModelCommand( Action handler )
		{
			_handler = handler;
		}


		#region ICommand Members

		public bool CanExecute( object parameter )
		{
			return true;
		}

	    /// <summary>
	    /// Occurs when changes occur that affect whether or not the command should execute.
	    /// </summary>
	    public event EventHandler CanExecuteChanged;

		public void Execute( object parameter )
		{
			_handler();
		}

		#endregion
	}

	/// <summary>
	/// Defines a command that can be bound to from XAML and redirects to a handler function.
	/// </summary>
	public class ViewModelCommand<T> : ICommand
		where T : class
	{
		private Action<T> _handler;


		public ViewModelCommand( Action<T> handler )
		{
			_handler = handler;
		}


		#region ICommand Members

		public bool CanExecute( object parameter )
		{
			return true;
		}

		public event EventHandler CanExecuteChanged;

		public void Execute( object parameter )
		{
			_handler( parameter as T );
		}

		#endregion
	}
}
