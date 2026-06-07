using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public class PermanentDeleteState
    {
        private PermanentDeleteState(bool canDelete, string message, DateTime allowedFromUtc, DateTime allowedUntilUtc)
        {
            CanDelete = canDelete;
            Message = message;
            AllowedFromUtc = allowedFromUtc;
            AllowedUntilUtc = allowedUntilUtc;
        }

        public bool CanDelete { get; }
        public string Message { get; }
        public DateTime AllowedFromUtc { get; }
        public DateTime AllowedUntilUtc { get; }

        public static PermanentDeleteState Allowed(string message, DateTime allowedFromUtc, DateTime allowedUntilUtc)
            => new(true, message, allowedFromUtc, allowedUntilUtc);

        public static PermanentDeleteState Blocked(string message, DateTime allowedFromUtc, DateTime allowedUntilUtc)
            => new(false, message, allowedFromUtc, allowedUntilUtc);
    }
}