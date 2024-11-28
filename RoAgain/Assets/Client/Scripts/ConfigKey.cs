
namespace Client
{
    /// <summary>
    /// Contains numeric values of server-side-stored config keys that this client understands
    /// </summary>
    // TODO: Move this into a configurable file to make it easier to adjust a client for different server-versions
    // e.g. one server has the Hotbar-config on keys 1-9, while another uses 101-109.
    // Adding new config values to be used by the client always needs code anyways, so for that it's not important
    public enum ConfigKey
    {
        Unknown = -1,
        Hotkey_BEGIN = Hotkey_Hotbar10,
        Hotkey_Hotbar10 = 1, // potentially used for 10th slot
        Hotkey_Hotbar11,
        Hotkey_Hotbar12,
        Hotkey_Hotbar13,
        Hotkey_Hotbar14,
        Hotkey_Hotbar15,
        Hotkey_Hotbar16,
        Hotkey_Hotbar17,
        Hotkey_Hotbar18,
        Hotkey_Hotbar19,
        Hotkey_Hotbar20, // potentially used for 10th slot
        Hotkey_Hotbar21,
        Hotkey_Hotbar22,
        Hotkey_Hotbar23,
        Hotkey_Hotbar24,
        Hotkey_Hotbar25,
        Hotkey_Hotbar26,
        Hotkey_Hotbar27,
        Hotkey_Hotbar28,
        Hotkey_Hotbar29,
        Hotkey_Hotbar30, // potentially used for 10th slot
        Hotkey_Hotbar31,
        Hotkey_Hotbar32,
        Hotkey_Hotbar33,
        Hotkey_Hotbar34,
        Hotkey_Hotbar35,
        Hotkey_Hotbar36,
        Hotkey_Hotbar37,
        Hotkey_Hotbar38,
        Hotkey_Hotbar39,
        Hotkey_Hotbar40, // potentially used for 10th slot
        Hotkey_Hotbar41,
        Hotkey_Hotbar42,
        Hotkey_Hotbar43,
        Hotkey_Hotbar44,
        Hotkey_Hotbar45,
        Hotkey_Hotbar46,
        Hotkey_Hotbar47,
        Hotkey_Hotbar48,
        Hotkey_Hotbar49,

        Hotkey_ToggleCharMainWindow,
        Hotkey_ToggleStatWindow,
        Hotkey_ToggleSkillWindow,
        Hotkey_ToggleHotbar,
        Hotkey_ToggleGameMenuWindow,

        Hotkey_ToggleChatInput,

        Hotkey_ConfirmDialog,
        Hotkey_ToggleInventoryWindow,
        Hotkey_ToggleEquipmentWindow,

        Hotkey_END = Hotkey_ToggleEquipmentWindow,

        SkillData_Hotbar10 = 200, // potentially used for 10th slot
        SkillData_Hotbar11,
        SkillData_Hotbar12,
        SkillData_Hotbar13,
        SkillData_Hotbar14,
        SkillData_Hotbar15,
        SkillData_Hotbar16,
        SkillData_Hotbar17,
        SkillData_Hotbar18,
        SkillData_Hotbar19,
        SkillData_Hotbar20, // potentially used for 10th slot
        SkillData_Hotbar21,
        SkillData_Hotbar22,
        SkillData_Hotbar23,
        SkillData_Hotbar24,
        SkillData_Hotbar25,
        SkillData_Hotbar26,
        SkillData_Hotbar27,
        SkillData_Hotbar28,
        SkillData_Hotbar29,
        SkillData_Hotbar30, // potentially used for 10th slot
        SkillData_Hotbar31,
        SkillData_Hotbar32,
        SkillData_Hotbar33,
        SkillData_Hotbar34,
        SkillData_Hotbar35,
        SkillData_Hotbar36,
        SkillData_Hotbar37,
        SkillData_Hotbar38,
        SkillData_Hotbar39,
        SkillData_Hotbar40, // potentially used for 10th slot
        SkillData_Hotbar41,
        SkillData_Hotbar42,
        SkillData_Hotbar43,
        SkillData_Hotbar44,
        SkillData_Hotbar45,
        SkillData_Hotbar46,
        SkillData_Hotbar47,
        SkillData_Hotbar48,
        SkillData_Hotbar49,

        // these values will get removed in favour of a server-selection system at some pont
        ServerIp = 300,
        ServerPort,
    }
}

