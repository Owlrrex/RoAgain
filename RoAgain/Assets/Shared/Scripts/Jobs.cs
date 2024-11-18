
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
}