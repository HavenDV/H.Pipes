using System;

namespace ExampleCLI
{
    [Serializable]
    class MyMessage
    {
        public int Id;
        public string Text;

        public override string ToString()
        {
            return string.Format("\"{0}\" (message ID = {1})", Text, Id);
        }
    }
}