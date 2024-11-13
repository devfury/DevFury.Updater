using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows;

namespace DevFury.AutoUpdate.Commons
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        internal FrameworkElement Owner { get; set; }

        internal Window OwnerWindow
        {
            get
            {
                if (Owner == null) return null;
                else if (Owner is Window win) return win;
                else return Window.GetWindow(Owner);
            }
        }

        /// <summary>
        /// 대화상자의 DialogResult 값을 가져오거나 설정 합니다.
        /// </summary>
        protected bool? DialogResult { set { if (OwnerWindow != null) { OwnerWindow.DialogResult = value; } } }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public virtual void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            if (property.Body is MemberExpression body)
            {
                OnPropertyChanged(body.Member.Name);
            }
            else
            {
                throw new ArgumentException("The body must be a member expression");
            }
        }
    }
}
