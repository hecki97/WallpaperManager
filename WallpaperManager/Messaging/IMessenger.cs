/*****************************************************************
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
    interface IMessenger
    {
        void Register<TNotification>(object recipient, Action<TNotification> action);
        void Register<TNotification>(object recipient, string identCode, Action<TNotification> action);

        void Send<TNotification>(TNotification notification);
        void Send<TNotification>(TNotification notification, string identCode);

        void Unregister<TNotification>(object recipient);
        void Unregister<TNotification>(object recipient, string identCode);
    }
}
