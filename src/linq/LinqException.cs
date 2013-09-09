using System;

namespace Kiss.Linq
{
    public class LinqException : KissException
    {
        public LinqException ( string message, Exception ex ) : base ( message, ex ) { }
        public LinqException ( string message ) : base ( message ) { }
    }
}
