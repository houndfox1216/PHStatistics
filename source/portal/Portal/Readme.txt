appsettings.json 參數說明
{
    "Cloudfun": {
        "Debug": false, // 啟用偵錯模式
        "Cookie": {
          "LifeSpan": "30.0:0:0" // 生命週期，數值為秒，字串為TimeSpan格式。預設為30天
        },
        "JsonWebToken": {
          "SecretKey": "YOUR SECRET KEY", // 簽名用金鑰。預設為 Application 的 Name
          "Algorithm": "HS256" // 演算法。預設為 HS256
        },
        "Authentication": {
          "Keepalive": "00:15:00", // 存活時間，超過時間未有活動將自動登出，預設為空
          "AccessDeniedRoute": "/Member/Login", // 驗證失敗時的導向路由，預設為空
          "ChangePasswordRoute": "/Member/ChangePassword", // 密碼變更時的導向路由，預設為空
          "ReturnUrlParameter": "returnUrl", // 身分驗證後返回網址之參數名稱，預設為 referer
          "PortalUser": { // 各種用戶可分開設定
            "AccessDeniedRoute": "/Member/Login", // 驗證失敗時的導向路由，預設為空
            "ChangePasswordRoute": "/Member/ChangePassword", // 密碼變更時的導向路由，預設為空
            "ReturnUrlParameter": "returnUrl", // 身分驗證後返回網址之參數名稱，預設為 referer
          }
        },
        "Culture": {
          "Default": "zh-TW", // 如有設置且未指定語系下，將使用此預設語系，反之由 .NET 判斷
          "RouteName": "culture", // 路由參數名稱，請於 route pattern 中加入類似 {culture} 字段
          "QueryName": "culture", // 查詢參數名稱，由 Query String 參數決定語系
          "SessionName": "culture", // Session 參數名稱，由 Session 參數決定語系，且 Culture 改變時會回寫到 Session
          "HeaderName": "culture", // Header 參數名稱，由 HTTP Header 參數決定語系
          "CookieName": "culture" // Cookie 參數名稱，由 Cookie 參數決定語系，且 Culture 改變時會設定 Cookie
        },
        "Network": {
          "Allowed": [ "127.0.0.1", "192.168.0.0/8", "{::1}" ], // 允許的 IP Addresses
          "Denied": "10.5.0.0/255.255.0.0" // 禁止的 IP Addresses
        },
        "Redis": {
            "Configuration": "127.0.0.1:6379,syncTimeout=3000"
        },
        "Directory": {
            "Temp": "~/files/temp" // 暫存目錄。預設為作業系統之暫存目錄，以 '@' 開頭表示在 Content Root 之下，以 '~' 開頭表示在 Web Root 之下
        },
        "NLog": {
            "Configuration": "NLog.config"
        },
        "GoogleAnalytics": {
            "Property": "349800223",
            "Credential": "@/credential.json"
        },
        "FileSystem": {
            "Root": "/files"
        }
    },
    "FTP": {
        "Host": "YOUR FTP HOST",
        "Username": "YOUR FTP USERNAME",
        "Password": "YOUR FTP PASSWORD"
    },
    "Version": "0.1.0.0" // 組態檔版本
}

任意節點之後的參數可以透過 ConfigurationBase.EncryptConfigurationValue 轉換為雜湊後的 base64 字串，於前綴加上 @64: 即可進行替換，舉例如下
"FTP": {
    "Host": "YOUR FTP HOST",
    "Username": "YOUR FTP USERNAME",
    "Password": "YOUR FTP PASSWORD"
}
等同
"FTP": "@64:lyagIkhvcmQiOiaiWU6VUi8GVFagS16TVCI4ICacVXZec3whbWUiOiaiWU6VUi8GVFagVVZFUkw8TUUi7CagIe8himZmbmJkIjogIeePVVIgReRQIF88UEZXTEJ1In0"
