﻿/*****************************************************************
 * Author: Norbert Eder
 * E-Mail: csharp@gmx.at
 * Blog  : http://devtyr.norberteder.com
 * 
 * Copyright © 2010 by Norbert Eder
 * 
 * ***************************************************************/

using System;

namespace DevTyr.Mvvm.Messaging
{
    public class WeakReferenceAction
    {
        private WeakReference target;
        private Action action;

        public WeakReferenceAction(object target, Action action)
        {
            this.target = new WeakReference(target);
            this.action = action;
        }

        public WeakReference Target
        {
            get
            {
                return target;
            }
        }

        public void Execute()
        {
            if (action != null && target != null && target.IsAlive)
                action.Invoke();
        }

        public void Unload()
        {
            target = null;
            action = null;
        }
    }

    public class WeakReferenceAction<T> : WeakReferenceAction, IActionParameter
    {
        private Action<T> action;

        public WeakReferenceAction(object target, Action<T> action)
            : base (target, null)
        {
            this.action = action;
        }


        public new void Execute()
        {
            if (action != null && Target != null && Target.IsAlive)
                action(default(T));
        }

        public void Execute(T parameter)
        {
            if (action != null && Target != null && Target.IsAlive)
                this.action(parameter);
        }

        public Action<T> Action
        {
            get
            {
                return action;
            }
        }

        #region IActionParameter Members

        public void ExecuteWithParameter(object parameter)
        {
            this.Execute((T)parameter);
        }

        #endregion
    }
}
