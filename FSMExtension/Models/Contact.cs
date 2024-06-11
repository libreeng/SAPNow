using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FSMExtension.Models
{
    public class Contact
    {
        public string Name { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public ContactRole Role { get; set; }

        public string? Email { get; set; }
    }

    public class ContactComparer : IEqualityComparer<Contact>
    {
        public bool Equals(Contact? x, Contact? y)
        {
            return string.Equals(x?.Email, y?.Email, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] Contact obj)
        {
            return obj.Email?.GetHashCode() ?? 0;
        }
    }
}
