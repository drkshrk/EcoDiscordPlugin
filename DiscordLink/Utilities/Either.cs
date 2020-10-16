﻿namespace Eco.Plugins.DiscordLink.Utilities
{
    public struct Either<T1, T2> where T1 : class where T2 : class
    {
        private readonly object _value;

        public Either(object value1)
        {
            _value = value1;
        }
        
        public TG Get<TG>() where TG : class
        {
            return _value as TG;
        }

        public bool Is<TG>()
        {
            return _value is TG;
        }

        public override bool Equals(object obj)
        {
            return obj is Either<T1, T2> && ((Either<T1, T2>) obj)._value == _value;
        }

        public bool Equals(Either<T1, T2> other)
        {
            return Equals(_value, other._value);
        }

        public override int GetHashCode()
        {
            return (_value != null ? _value.GetHashCode() : 0);
        }
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> upstream/master
