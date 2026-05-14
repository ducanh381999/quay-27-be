using Quay27.Domain.Entities;

namespace Quay27.Infrastructure.Persistence;

/// <summary>Seed rows for <see cref="BankCatalogItem"/> (no legacy external Ids).</summary>
internal static class TreasuryBankCatalogSeedData
{
    public static IReadOnlyList<BankCatalogItem> BuildItems()
    {
        var rows = new List<(string Code, string FullName, string? GlobalName, int OrderNum, string SearchText, int? CountryId)>
        {
            ("VIB", "Ngân hàng TMCP Quốc Tế Việt Nam", "Vietnam International Commercial Joint Stock Bank", 1, "vib ngan hang tmcp quoc te viet nam", 1),
            ("MBV", "Ngân hàng TNHH MTV Việt Nam Hiện Đại", "Modern Bank of Vietnam Limited", 1, "mbv ngan hang tnhh mtv viet nam hien dai", 1),
            ("Vietcombank", "Ngân hàng TMCP Ngoại thương Việt Nam", "Bank for Foreign trade of Vietnam", 2, "vietcombank ngan hang tmcp ngoai thuong viet nam", 1),
            ("VietinBank", "Ngân hàng TMCP Công Thương Việt Nam", "VietNam Joint Stock Commercial Bank for Industry and Trade", 3, "vietinbank ngan hang tmcp cong thuong viet nam", 1),
            ("BIDV", "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam", "Joint Stock Commercial Bank for Investment and Development of Vietnam", 4, "bidv ngan hang tmcp dau tu va phat trien viet nam", 1),
            ("Techcombank", "Ngân hàng TMCP Kỹ Thương Việt Nam", "Vietnam Technological and Commercial Joint- stock Bank", 5, "techcombank ngan hang tmcp ky thuong viet nam", 1),
            ("ACB", "Ngân hàng Á Châu", "Asia Commercial Joint Stock Bank", 6, "acb ngan hang a chau", 1),
            ("MB", "Ngân hàng TMCP Quân Đội", "Military Commercial Joint Stock Bank", 7, "mb ngan hang tmcp quan doi", 1),
            ("VPBank", "Ngân hàng TMCP Việt Nam Thịnh Vượng", "Vietnam Prosperity Joint-Stock Commercial Bank", 8, "vpbank ngan hang tmcp viet nam thinh vuong", 1),
            ("Agribank", "Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam", "Vietnam Bank for Agriculture and Rural Development", 9, "agribank ngan hang nong nghiep va phat trien nong thon viet nam", 1),
            ("SHB", "Ngân hàng TMCP Sài Gòn – Hà Nội", "Saigon – Hanoi Commercial Joint Stock Bank", 10, "shb ngan hang tmcp sai gon – ha noi", 1),
            ("Sacombank", "Ngân hàng TMCP Sài Gòn Thương Tín", "Saigon Thuong Tin Commercial Joint Stock Bank", 11, "sacombank ngan hang tmcp sai gon thuong tin", 1),
            ("HDB", "Ngân hàng TMCP phát triển Tp.HCM", "Development Joint Stock Commercial Bank", 12, "hdb ngan hang tmcp phat trien tp.hcm", 1),
            ("PVCombank", "Ngân hàng TMCP Đại Chúng Việt Nam", "Vietnam Public Joint-stock Commercial Bank", 13, "pvcombank ngan hang tmcp dai chung viet nam", 1),
            ("Oceanbank", "Ngân hàng TM TNHH Một Thành Viên Đại Dương", "Ocean Commercial One Member Limited Liability Bank", 14, "oceanbank ngan hang tm tnhh mot thanh vien dai duong", 1),
            ("LienVietPostBank", "Ngân hàng Thương mại cổ phần Lộc Phát Việt Nam", "Fortune Vietnam Joint Stock Commercial Bank", 15, "lienvietpostbank ngan hang thuong mai co phan loc phat viet nam", 1),
            ("TPBank", "Ngân hàng TMCP Tiên Phong", "Tien Phong Commercial Joint Stock Bank", 16, "tpbank ngan hang tmcp tien phong", 1),
            ("MSB Maritime Bank", "Ngân hàng TMCP Hàng Hải Việt Nam", "Vietnam Maritime Commercial Joint Stock Bank", 17, "msb maritime bank ngan hang tmcp hang hai viet nam", 1),
            ("Eximbank", "Ngân hàng TMCP Xuất nhập khẩu Việt Nam", "Vietnam Export Import Bank", 18, "eximbank ngan hang tmcp xuat nhap khau viet nam", 1),
            ("ABBank", "Ngân hàng TMCP An Bình", "An Binh Commercial Joint Stock Bank", 19, "abbank ngan hang tmcp an binh", 1),
            ("OCB", "Ngân hàng TMCP Phương Đông", "Orient Commercial Joint Stock Bank", 20, "ocb ngan hang tmcp phuong dong", 1),
            ("NASB", "Ngân hàng TMCP Bắc Á", "North American State Bank", 21, "nasb ngan hang tmcp bac a", 1),
            ("DongA Bank", "Ngân hàng TMCP Đông Á", "DongA Joint Stock Commercial Bank", 22, "donga bank ngan hang tmcp dong a", 1),
            ("VietABank", "Ngân hàng TMCP Việt Á", "VietABank", 23, "vietabank ngan hang tmcp viet a", 1),
            ("BAOVIET Bank", "Ngân hàng TMCP Bảo Việt", "BAOVIET Bank", 24, "baoviet bank ngan hang tmcp bao viet", 1),
            ("SAIGONBANK", "NGÂN HÀNG TMCP SÀI GÒN CÔNG THƯƠNG", "SAIN BANK FOR INDUSTRY AND TRADE", 25, "saigonbank ngan hang tmcp sai gon cong thuong", 1),
            ("NamABank", "Ngân hàng TMCP Nam Á", "Nam A Commercial Joint Stock Bank", 26, "namabank ngan hang tmcp nam a", 1),
            ("GPBank", "Ngân hàng TM TNHH MTV Dầu Khí Toàn Cầu.", "Global Petro Sole Member Limited Commercial Bank", 27, "gpbank ngan hang tm tnhh mtv dau khi toan cau.", 1),
            ("KienLongBank", "Kiên Long", "Kien Long Bank", 28, "kienlongbank kien long", 1),
            ("VCCB", "Bản Việt", "VIET CAPITAL BANK", 29, "vccb ban viet", 1),
            ("PG Bank", "Xăng dầu Petrolimex", "Petrolimex Group Bank", 30, "pg bank xang dau petrolimex", 1),
            ("Co-opBank", "Ngân hàng Hợp tác xã Việt Nam", "Co-operative bank of VietNam", 33, "co-opbank ngan hang hop tac xa viet nam", 1),
            ("NCB", "NH TMCP Quốc Dân", "National Citizen Commercial Joint Stock Bank", 35, "ncb nh tmcp quoc dan", 1),
            ("SCB", "Ngân hàng TMCP Sài Gòn", "Sai Gon Commercial Bank", 37, "scb ngan hang tmcp sai gon", 1),
            ("VietBank", "Ngân Hàng TMCP Việt Nam Thương Tín", "Vietnam Thuong Tin Commercial Joint Stock Bank", 38, "vietbank ngan hang tmcp viet nam thuong tin", 1),
            ("Deutsche Bank", "Deutsche Bank Việt Nam", "Deutsche Bank AG, Vietnam", 41, "deutsche bank deutsche bank viet nam", 1),
            ("Citibank", "Ngân hàng Citibank Việt Nam", "Citibank VietNam", 42, "citibank ngan hang citibank viet nam", 1),
            ("HSBC", "Ngân hàng TNHH MTV HSBC Việt Nam", "HSBC Bank Vietnam Limited", 43, "hsbc ngan hang tnhh mtv hsbc viet nam", 1),
            ("Standard Chartered", "Standard Chartered", "Standard Chartered Bank (Vietnam) Limited", 44, "standard chartered standard chartered", 1),
            ("SHBVN", "Ngân hàng TNHH MTV Shinhan Việt Nam", "Shinhan Vietnam Bank Limited", 45, "shbvn ngan hang tnhh mtv shinhan viet nam", 1),
            ("HLBVN", "Ngân hàng Hong Leong Việt Nam", "Hong Leong Bank Vietnam Limited ", 46, "hlbvn ngan hang hong leong viet nam", 1),
            ("Mizuho", "Ngân hàng Mizuho", "Mizuho bank", 49, "mizuho ngan hang mizuho", 1),
            ("MUFG", "MUFG Bank", "MUFG Bank", 50, "mufg mufg bank", 1),
            ("SMBC", "Ngân hàng Sumitomo Mitsui", "Sumitomo Mitsui Banking Corporation", 51, "smbc ngan hang sumitomo mitsui", 1),
            ("IVB", "Ngân hàng TNHH Indovina", "Indovina Bank Ltd", 52, "ivb ngan hang tnhh indovina", 1),
            ("VRB", "Ngân hàng Liên doanh Việt - Nga", "Vietnam - Russia Joint Venture Bank", 53, "vrb ngan hang lien doanh viet - nga", 1),
            ("PBVN", "Ngân hàng Public Việt Nam", "Public Bank Vietnam", 55, "pbvn ngan hang public viet nam", 1),
            ("SEAB", "TMCP Đông Nam Á", "SouthEast Asia Commercial Joint Stock Bank", 59, "seab tmcp dong nam a", 1),
            ("CBB", "TM TNHH MTV Xây Dựng Việt Nam", "Construction Commercial One Member Limited Liability Bank", 60, "cbb tm tnhh mtv xay dung viet nam", 1),
            ("CIMB", "Ngân hàng CIMB Việt Nam", "CIMB Bank Vietnam Limited", 61, "cimb ngan hang cimb viet nam", 1),
            ("UOB", "Ngân hàng UOB Việt Nam", "UOB Vietnam Limited", 62, "uob ngan hang uob viet nam", 1),
            ("WVN", "TNHH MTV Woori Việt Nam", "Woori Bank Vietnam Limited", 63, "wvn tnhh mtv woori viet nam", 1),
            ("KB HN", "Kookmin - Chi nhánh Hà Nội", null, 64, "kb hn kookmin - chi nhanh ha noi", 1),
            ("KB HCM", "Kookmin - Chi nhánh HCM", null, 65, "kb hcm kookmin - chi nhanh hcm", 1),
            ("NHB HN", "NONGHYUP - Chi nhánh Hà Nội", null, 66, "nhb hn nonghyup - chi nhanh ha noi", 1),
            ("DBS", "DBS - chi nhánh HCM", null, 67, "dbs dbs - chi nhanh hcm", 1),
            ("SGICB", "TMCP Sài Gòn Công Thương", null, 68, "sgicb tmcp sai gon cong thuong", 1),
            ("IBK HN", "IBK - chi nhánh Hà Nội", null, 69, "ibk hn ibk - chi nhanh ha noi", 1),
            ("IBK HCM", "IBK - chi nhánh HCM", null, 70, "ibk hcm ibk - chi nhanh hcm", 1),
            ("CAKE", "Ngân hàng số CAKE by VPBank", null, 71, "cake ngan hang so cake by vpbank", 1),
            ("UBANK", "Ngân hàng số Ubank by VPBank", null, 72, "ubank ngan hang so ubank by vpbank", 1),
            ("KBANK", "Đại chúng TNHH Kasikornbank", null, 73, "kbank dai chung tnhh kasikornbank", 1),
            ("CTBC", "Ngân hàng TNHH CTBC", "CTBC Bank", 74, "ctbc ngan hang tnhh ctbc", 1),
        };

        return rows.Select(r => new BankCatalogItem
        {
            Code = r.Code,
            FullName = r.FullName,
            GlobalName = r.GlobalName,
            OrderNum = r.OrderNum,
            SearchText = r.SearchText,
            IsActive = true,
            CountryId = r.CountryId,
        }).ToList();
    }
}
