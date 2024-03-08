using System;
using System.Collections;
using System.Collections.Generic;

namespace Piot.Discoid
{
    public class EnumeratorWrapper<T> : IEnumerable<T>
    {
        private readonly IEnumerator<T> enumerator;

        public EnumeratorWrapper(IEnumerator<T> enumerator)
        {
            this.enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}