namespace Xamarin.MacDev;

#if !POBJECT_INTERNAL
public
#endif
    class PBoolean : PValueObject<bool>
{
    public PBoolean(bool value) : base(value)
    {
    }

    public override PObject Clone()
    {
        return new PBoolean(Value);
    }

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			return NSNumber.FromBoolean (Value);
		}
#endif

    public override PObjectType Type
    {
        get { return PObjectType.Boolean; }
    }

    public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
    {
        const StringComparison ic = StringComparison.OrdinalIgnoreCase;

        if ("true".Equals(text, ic) || "yes".Equals(text, ic))
        {
            Value = true;
            return true;
        }

        if ("false".Equals(text, ic) || "no".Equals(text, ic))
        {
            Value = false;
            return true;
        }

        return false;
    }
}