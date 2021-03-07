using System;
using Nancy.Cookies;

namespace Lidarr.Http.Authentication
{
    public class LidarrNancyCookie : NancyCookie
    {
        public LidarrNancyCookie(string name, string value)
            : base(name, value)
        {
        }

        public LidarrNancyCookie(string name, string value, DateTime expires)
            : base(name, value, expires)
        {
        }

        public LidarrNancyCookie(string name, string value, bool httpOnly)
            : base(name, value, httpOnly)
        {
        }

        public LidarrNancyCookie(string name, string value, bool httpOnly, bool secure)
            : base(name, value, httpOnly, secure)
        {
        }

        public LidarrNancyCookie(string name, string value, bool httpOnly, bool secure, DateTime? expires)
            : base(name, value, httpOnly, secure, expires)
        {
        }

        public override string ToString()
        {
            return base.ToString() + "; SameSite=Strict";
        }
    }
}
