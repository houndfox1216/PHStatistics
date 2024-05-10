using System;
using System.ComponentModel;
using System.Framework;
using System.Framework.Data;
using System.Framework.Globalization;
using System.Framework.Payment;

namespace EmptyProject {
    /// <summary>
    /// 授權資料
    /// </summary>
    [Description("授權資料")]
    public class AuthData : IAuthData {
        #region IAuthData members

        string IAuthData.Token => Pan;

        #endregion

        private string pan;

        /// <summary>
        /// 卡號。
        /// </summary>
        public string Pan {
            get => pan;
            set => pan = value != null && CheckCardNumber(value) ? value : throw new FrameworkException("510, 信用卡卡號不正確");
        }

        /// <summary>
        /// 驗證碼。以信用卡而言為CVV2或CVC2
        /// </summary>
        public string ValidationCode { get; set; }

        /// <summary>
        /// 到期日期
        /// </summary>
        public DateTime? Expiry { get; set; }

        /// <summary>
        /// 貨幣
        /// </summary>
        public CurrencyCode Currency => CurrencyCode.TWD;

        /// <summary>
        /// 金額
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 是否為手機裝置
        /// </summary>
        public bool IsMobileDevice { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        public PaymentType PaymentType { get; set; }

        /// <summary>
        /// 品項名稱
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 說明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 檢查卡號
        /// </summary>
        /// <param name="pan">卡號</param>
        /// <returns>正確或不正確</returns>
        private static bool CheckCardNumber(string pan) {
            if (pan == null || pan.Length != 16) return false; // 卡號長度為16碼

            int evenum = 0; // 偶數值
            int oddnum = 0; // 奇數值
            int chksum = 0; // 檢查碼

            // 計算偶數值
            for (int i = 1; i < 15; i += 2) {
                char c = pan[i];
                if (!Char.IsDigit(c)) return false; // 卡號必為數字型態

                int temp = c - '0';
                evenum += temp;
            }

            // 計算奇數值
            for (int i = 0; i < 15; i = i + 2) {
                char c = pan[i];
                if (!Char.IsDigit(c)) return false; // 卡號必為數字型態

                int temp = (c - '0') * 2;
                if (temp > 9) temp = 1 + (temp - 10);
                oddnum = oddnum + temp;
            }

            chksum = evenum + oddnum;
            chksum = (10 - (chksum % 10)) % 10;

            char ch = pan[15];
            if (!Char.IsDigit(ch)) return false; // 卡號必為數字型態

            if (chksum == (pan[15] - '0'))
                return true;

            return false;
        }
    }
}