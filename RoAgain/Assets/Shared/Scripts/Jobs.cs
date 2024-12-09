
using Shared;
using System;
using System.Runtime.CompilerServices;

namespace Shared
{
    public enum JobId
    {
        Unknown = 0,
        Novice = 1,
        // 1st
        Swordman,
        Mage,
        Acolyte,
        Thief,
        Archer,
        Merchant,
        Taekwon,
        // 1st without 2nd
        Gunslinger = 40,
        Ninja,
        SuperNovice,
        // 1st Trans
        HighNovice = 51,
        HighSwordman,
        HighMage,
        HighAcolyte,
        HighThief,
        HighArcher,
        HighMerchant,
        // 2nd
        Knight = 101,
        Crusader,
        Wizard,
        Sage,
        Priest,
        Monk,
        Assassin,
        Rogue,
        Hunter,
        Bard,
        Dancer,
        Blacksmith,
        Alchemist,
        // 2nd without Trans
        StarGladiator = 180,
        SoulLinker,
        // 2nd Trans
        LordKnight = 201,
        Paladin,
        HighWizard,
        Professor,
        HighPriest,
        Champion,
        AssassinCross,
        Stalker,
        Sniper,
        Minstrel,
        Gypsy,
        Whitesmith,
        Creator,
    }
}

public static class JobIdExtensions
{
    public static bool IsAdvancementOf(this JobId self, JobId baseJob)
    {
        if (self == baseJob)
            return true;

        if (baseJob == JobId.Novice)
            return true;

        // All novices and First jobs are covered by the above two conditions

        switch(self)
        {
            case JobId.Knight:
            case JobId.Crusader:
                return baseJob == JobId.Swordman;
            case JobId.Wizard:
            case JobId.Sage:
                return baseJob == JobId.Mage;
            case JobId.Assassin:
            case JobId.Rogue:
                return baseJob == JobId.Thief;
            case JobId.Hunter:
            case JobId.Bard:
            case JobId.Dancer:
                return baseJob == JobId.Archer;
            case JobId.Priest:
            case JobId.Monk:
                return baseJob == JobId.Acolyte;
            case JobId.Blacksmith:
            case JobId.Alchemist:
                return baseJob == JobId.Merchant;
            // Trans jobs
            case JobId.HighSwordman:
                return baseJob == JobId.Swordman;
            case JobId.HighMage:
                return baseJob == JobId.Mage;
            case JobId.HighThief:
                return baseJob == JobId.Thief;
            case JobId.HighArcher:
                return baseJob == JobId.Archer;
            case JobId.HighAcolyte:
                return baseJob == JobId.Acolyte;
            case JobId.HighMerchant:
                return baseJob == JobId.Merchant;

            case JobId.LordKnight:
                return baseJob == JobId.Knight
                    || baseJob == JobId.Swordman
                    || baseJob == JobId.HighSwordman;
            case JobId.Paladin:
                return baseJob == JobId.Crusader
                    || baseJob == JobId.Swordman
                    || baseJob == JobId.HighSwordman;
            case JobId.HighWizard:
                return baseJob == JobId.Wizard
                    || baseJob == JobId.Mage
                    || baseJob == JobId.HighMage;
            case JobId.Professor:
                return baseJob == JobId.Sage
                    || baseJob == JobId.Mage
                    || baseJob == JobId.HighMage;
            case JobId.AssassinCross:
                return baseJob == JobId.Assassin
                    || baseJob == JobId.Thief
                    || baseJob == JobId.HighThief;
            case JobId.Stalker:
                return baseJob == JobId.Rogue
                    || baseJob == JobId.Thief
                    || baseJob == JobId.HighThief;
            case JobId.Sniper:
                return baseJob == JobId.Hunter
                    || baseJob == JobId.Archer
                    || baseJob == JobId.HighArcher;
            case JobId.Minstrel:
                return baseJob == JobId.Bard
                    || baseJob == JobId.Archer
                    || baseJob == JobId.HighArcher;
            case JobId.Gypsy:
                return baseJob == JobId.Dancer
                    || baseJob == JobId.Archer
                    || baseJob == JobId.HighArcher;
            case JobId.HighPriest:
                return baseJob == JobId.Priest
                    || baseJob == JobId.Acolyte
                    || baseJob == JobId.HighAcolyte;
            case JobId.Champion:
                return baseJob == JobId.Monk
                    || baseJob == JobId.Acolyte
                    || baseJob == JobId.HighAcolyte;
            case JobId.Whitesmith:
                return baseJob == JobId.Blacksmith
                    || baseJob == JobId.Merchant
                    || baseJob == JobId.HighMerchant;
            case JobId.Creator:
                return baseJob == JobId.Alchemist
                    || baseJob == JobId.Merchant
                    || baseJob == JobId.HighMerchant;
            default:
                return false;
        }
    }

    public static LocalizedStringId LocalizedName(this JobId jobId, bool isFemale)
    {
        return jobId switch
        {
            JobId.Novice => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            JobId.Swordman => isFemale ? new LocalizedStringId(251) : new LocalizedStringId(252),
            JobId.Mage => isFemale ? new LocalizedStringId(253) : new LocalizedStringId(254),
            JobId.Acolyte => isFemale ? new LocalizedStringId(255) : new LocalizedStringId(256),
            JobId.Thief => isFemale ? new LocalizedStringId(257) : new LocalizedStringId(258),
            JobId.Archer => isFemale ? new LocalizedStringId(259) : new LocalizedStringId(260),
            JobId.Merchant => isFemale ? new LocalizedStringId(261) : new LocalizedStringId(262),
            JobId.Taekwon => isFemale ? new LocalizedStringId(263) : new LocalizedStringId(264),
            //JobId.Gunslinger => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Ninja => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.SuperNovice => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighNovice => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighSwordman => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighMage => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighAcolyte => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighThief => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighArcher => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighMerchant => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Knight => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Crusader => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Wizard => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Sage => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Priest => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Monk => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Assassin => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Rogue => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Hunter => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Bard => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Dancer => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Blacksmith => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Alchemist => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.StarGladiator => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.SoulLinker => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.LordKnight => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Paladin => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighWizard => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Professor => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.HighPriest => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Champion => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.AssassinCross => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Stalker => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Sniper => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Minstrel => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Gypsy => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Whitesmith => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            //JobId.Creator => isFemale ? new LocalizedStringId(249) : new LocalizedStringId(250),
            _ => LocalizedStringId.INVALID,
        };
    }
}