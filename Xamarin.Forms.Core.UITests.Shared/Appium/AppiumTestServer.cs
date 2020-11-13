using System;
using Xamarin.UITest;

namespace Xamarin.Forms.Core.UITests.Appium
{
    internal class AppiumTestServer : ITestServer
    {
        protected AppiumTestServer() { }

        public virtual string Get(string endpoint)
        {
            throw new NotImplementedException();
        }

        public virtual string Post(string endpoint, object arguments = null)
        {
            throw new NotImplementedException();
        }

        public virtual string Put(string endpoint, byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}