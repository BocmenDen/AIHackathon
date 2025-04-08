﻿using AIHackathon.DB.Models;
using PhoneNumbers;
using System.Text.RegularExpressions;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public partial class SetPhonePage(HandlePageRouter pageRouter) : SetValuePageBase(pageRouter)
    {
        public const string Key = "SetPhonePage";

        private readonly static PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        protected override string MessageStart => "Введите номер телефона";
        protected override string MessageNotCorrect => "Введённые данные номера телефона не являются корректными";

        protected override bool IsCorrectValue(string? value) => value != null && GetValidatorNumber().IsMatch(value) && PhoneUtil.IsValidNumber(PhoneUtil.Parse(value, ConstsShared.DefaultRegion));

        protected override void SaveValue(User user, string? value) => RegisterModel.Phone = value;
        [GeneratedRegex("^\\+?[1-9][0-9]{7,14}$")]
        private static partial Regex GetValidatorNumber();
    }
}
