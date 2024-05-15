
namespace Client
{
    /// <summary>
    /// Contains numeric values of server-side-stored config keys that this client understands
    /// </summary>
    // TODO: Move this into a configurable file to make it easier to adjust a client for different server-versions
    // e.g. one server has the Hotbar-config on keys 1-9, while another uses 101-109.
    // Adding new config values to be used by the client always needs code anyways, so for that it's not important
    public enum RemoteConfigKey
    {
        Unknown = -1
    }
}

