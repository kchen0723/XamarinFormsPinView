﻿using System;
using System.Collections.Generic;
using System.Windows.Input;
using Xamarin.Forms;

namespace FormsPinView.Core
{
    /// <summary>
    /// The PIN view.
    /// </summary>
    public partial class PinView : Grid
    {
        #region Private fields and properties

        private const int DefaultPinLength = 4;
        private const string DefaultEmptyCircleImage = "img_circle.png";
        private const string DefaultFilledCircleImage = "img_circle_filled.png";

        #endregion

        #region Events

        /// <summary>
        /// Occurs when user enters a corrct PIN.
        /// </summary>
        public event EventHandler<EventArgs> Success;

        /// <summary>
        /// Occurs when user enters an incorrect pin.
        /// </summary>
        public event EventHandler<EventArgs> Error;

        /// <summary>
        /// Occurs when user presses a button and displayed text is updated.
        /// </summary>
        public event EventHandler<EventArgs> DisplayedTextUpdated;

        #endregion

        #region Bindable properties

        public static readonly BindableProperty PinLengthProperty =
            BindableProperty.Create(propertyName: nameof(PinLength),
                                    returnType: typeof(int),
                                    declaringType: typeof(PinView),
                                    defaultValue: DefaultPinLength,
                                    propertyChanged: HandlePinLengthChanged);

        /// <summary>
        /// Gets or sets the length of the PIN.
        /// </summary>
        public int PinLength
        {
            get { return (int)GetValue(PinLengthProperty); }
            set 
            { 
                if ((int)value <= 0)
                {
                    throw new ArgumentException("TargetPinLength must be a positive value");
                }
                SetValue(PinLengthProperty, value);
            }
        }

        private static void HandlePinLengthChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if ((int)newValue <= 0)
            {
                throw new ArgumentException("TargetPinLength must be a positive value");
            }
            ((PinView)bindable).EnteredPin.Clear();
            ((PinView)bindable).UpdateDisplayedText(resetUI: true);
        }

        public static readonly BindableProperty EmptyCircleImageProperty =
            BindableProperty.Create(propertyName: nameof(EmptyCircleImage),
                                    returnType: typeof(ImageSource),
                                    declaringType: typeof(PinView),
                                    defaultValue: new FileImageSource { File = DefaultEmptyCircleImage },
                                    propertyChanged: HandleCircleImageChanged);

        /// <summary>
        /// Gets or sets the ImageSource of the <i>empty</i> item icon.
        /// </summary>
        public ImageSource EmptyCircleImage
        {
            get { return (ImageSource)GetValue(EmptyCircleImageProperty); }
            set
            {
                SetValue(EmptyCircleImageProperty, value);

            }
        }

        public static readonly BindableProperty FilledCircleImageProperty =
            BindableProperty.Create(propertyName: nameof(FilledCircleImage),
                                    returnType: typeof(ImageSource),
                                    declaringType: typeof(PinView),
                                    defaultValue: new FileImageSource { File = DefaultFilledCircleImage },
                                    propertyChanged: HandleCircleImageChanged);

        /// <summary>
        /// Gets or sets the ImageSource of the <i>filled</i> item icon.
        /// </summary>
        public ImageSource FilledCircleImage
        {
            get { return (ImageSource)GetValue(FilledCircleImageProperty); }
            set
            {
                SetValue(FilledCircleImageProperty, value);
                UpdateDisplayedText(resetUI: true);
            }
        }

        private static void HandleCircleImageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((PinView)bindable).UpdateDisplayedText(resetUI: true);
        }

        public static readonly BindableProperty ValidatorProperty =
            BindableProperty.Create(propertyName: nameof(Validator),
                                    returnType: typeof(Func<IList<char>, bool>),
                                    declaringType: typeof(PinView),
                                    defaultValue: null);

        /// <summary>
        /// Gets or sets the validator function.
        /// </summary>
        public Func<IList<char>, bool> Validator
        {
            get { return (Func<IList<char>, bool>)GetValue(ValidatorProperty); }
            set { SetValue(ValidatorProperty, value); }
        }

        public static readonly BindableProperty SuccessCommandProperty =
            BindableProperty.Create(propertyName: nameof(SuccessCommand),
                                    returnType: typeof(ICommand),
                                    declaringType: typeof(PinView),
                                    defaultValue: null);

        /// <summary>
        /// Gets or sets a command which invokes when the correct PIN is entered.
        /// </summary>
        public ICommand SuccessCommand
        {
            get { return (ICommand)GetValue(SuccessCommandProperty); }
            set { SetValue(SuccessCommandProperty, value); }
        }

        public static readonly BindableProperty ErrorCommandProperty =
            BindableProperty.Create(propertyName: nameof(ErrorCommand),
                                    returnType: typeof(ICommand),
                                    declaringType: typeof(PinView),
                                    defaultValue: null);

        /// <summary>
        /// Gets or sets a command which invokes when an incorrect PIN is entered.
        /// </summary>
        public ICommand ErrorCommand
        {
            get { return (ICommand)GetValue(ErrorCommandProperty); }
            set { SetValue(ErrorCommandProperty, value); }
        }

        #endregion

        #region Properties

        private IList<char> _enteredPin = new List<char>();

        /// <summary>
        /// Gets or sets the entered PIN.
        /// </summary>
        /// <value>The entered pin.</value>
        public IList<char> EnteredPin
        {
            get { return _enteredPin; }
            set
            {
                _enteredPin = value;
                RaisePropertyChanged(nameof(EnteredPin));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entered PIN should be cleaned or not
        /// after it was confirmed as correct. Default is <code>true</code>.
        /// </summary>
        public bool ClearAfterSuccess { get; set; } = true;

        /// <summary>
        /// Gets the "key pressed" command.
        /// </summary>
        public Command<string> KeyPressedCommand { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FormsPinView.Core.PinView"/> class.
        /// </summary>
        public PinView()
        {
            InitializeComponent();

            KeyPressedCommand = new Command<string>(arg =>
            {
                if (Validator == null)
                {
                    throw new InvalidOperationException($"{nameof(Validator)} is not set");
                }

                if (arg == "Backspace")
                {
                    if (EnteredPin.Count > 0)
                    {
                        EnteredPin.RemoveAt(EnteredPin.Count - 1);
                        UpdateDisplayedText(resetUI: false);
                    }
                }
                else if (EnteredPin.Count < PinLength)
                {
                    EnteredPin.Add(arg[0]);
                    if (EnteredPin.Count == PinLength)
                    {
                        if (Validator.Invoke(EnteredPin))
                        {
                            if (ClearAfterSuccess)
                            {
                                EnteredPin.Clear();
                            }
                            // fill the last PIN symbol image
                            UpdateDisplayedText(resetUI: false);
                            // Raise Success event
                            RaiseSuccess();
                        }
                        else
                        {
                            // clear all PIN symbols
                            EnteredPin.Clear();
                            UpdateDisplayedText(resetUI: false);
                            // Raise Error event
                            RaiseError();
                        }
                    }
                    else
                    {
                        UpdateDisplayedText(resetUI: false);
                    }
                }
            });

            foreach (var view in Children)
            {
                if (view is PinItemView pinItem)
                {
                    pinItem.Command = KeyPressedCommand;
                }
            }

            UpdateDisplayedText(resetUI: true);
        }

        /// <summary>
        /// Updates the displayed PIN icons.
        /// </summary>
        protected void UpdateDisplayedText(bool resetUI)
        {
            if (PinLength <= 0)
            {
                // not expected to happen
                throw new InvalidOperationException($"{PinLengthProperty.PropertyName} property" +
                                                    $" is not a positive value but {PinLength}");
            }

            if (resetUI || circlesStackLayout.Children.Count == 0)
            {
                circlesStackLayout.Children.Clear();
                for (int i = 0; i < PinLength; ++i)
                {
                    circlesStackLayout.Children.Add(new Image
                    {
                        Source = EmptyCircleImage,
                        HeightRequest = 28,
                        WidthRequest = 28,
                        MinimumWidthRequest = 28,
                        MinimumHeightRequest = 28
                    });
                }
            }

            if (EnteredPin != null)
            {
                for (int i = 0; i < EnteredPin.Count; ++i)
                {
                    (circlesStackLayout.Children[i] as Image).Source = FilledCircleImage;
                }
                for (int i = EnteredPin.Count; i < PinLength; ++i)
                {
                    (circlesStackLayout.Children[i] as Image).Source = EmptyCircleImage;
                }
            }

            DisplayedTextUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// "Shakes" the PIN view and raises the <code>Success</code> event.
        /// </summary>
        protected void RaiseError()
        {
            this.AbortAnimation("shake");
            this.Animate("shake",
                         (arg) =>
                         {
                             var shift = Math.Sin(2 * 2 * Math.PI * arg);
                             this.TranslationX = 6 * shift;
                         },
                         16 * 4,
                         250,
                         Easing.Linear,
                         (arg1, arg2) =>
                         {
                             this.TranslationX = 0;
                         });

            var error = Error;
            var errorCommand = ErrorCommand;

            if (error != null)
                error.Invoke(this, EventArgs.Empty);
            
            if (errorCommand != null && errorCommand.CanExecute(null))
            {
                errorCommand.Execute(null);
            }
        }

        /// <summary>
        /// Raises the <code>Success</code> event.
        /// </summary>
        protected void RaiseSuccess()
        {
            var success = Success;
            var successCommand = SuccessCommand;

            if (success != null)
                success.Invoke(this, EventArgs.Empty);

            if (successCommand != null && successCommand.CanExecute(null))
            {
                successCommand.Execute(null);
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyNames">Properties names.</param>
        protected void RaisePropertyChanged(params string[] propertyNames)
        {
            if (propertyNames == null)
            {
                return;
            }
            foreach (string propertyName in propertyNames)
            {
                base.OnPropertyChanged(propertyName);
            }
        }
    }
}
