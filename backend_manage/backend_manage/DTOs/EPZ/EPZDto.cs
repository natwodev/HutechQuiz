using System.Collections.Generic;
using System.Xml.Serialization;

namespace backend_manage.DTOs.EPZ
{
    [XmlRoot("DeThi")]
    public class EPZDto
    {
        [XmlElement("MonHoc")]
        public MonHocDto MonHoc { get; set; }
    }

    public class MonHocDto
    {
        [XmlElement("MaMonHoc")]
        public string MaMonHoc { get; set; }
        [XmlElement("MaSoMonHoc")]
        public string MaSoMonHoc { get; set; }
        [XmlElement("TenMonHoc")]
        public string TenMonHoc { get; set; }
        [XmlElement("TongSoCauLay")]
        public int TongSoCauLay { get; set; }
        [XmlElement("Phan")]
        public List<PhanDto> Phan { get; set; }
    }

    public class PhanDto
    {
        [XmlElement("MaPhan")]
        public string MaPhan { get; set; }
        [XmlElement("TenPhan")]
        public string TenPhan { get; set; }
        [XmlElement("NoiDung")]
        public string NoiDung { get; set; }
        [XmlElement("MaMonHoc")]
        public string MaMonHoc { get; set; }
        [XmlElement("MaSoPhan")]
        public string MaSoPhan { get; set; }
        [XmlElement("MaPhanCha")]
        public string MaPhanCha { get; set; }
        [XmlElement("SoCauLay")]
        public int SoCauLay { get; set; }
        [XmlElement("CauHoi")]
        public List<CauHoiDto> CauHoi { get; set; }
    }

    public class CauHoiDto
    {
        [XmlElement("MaCauHoi")]
        public string MaCauHoi { get; set; }
        [XmlElement("MaPhan")]
        public string MaPhan { get; set; }
        [XmlElement("MaSoCauHoi")]
        public string MaSoCauHoi { get; set; }
        [XmlElement("NoiDung")]
        public string NoiDung { get; set; }
        [XmlElement("HoanVi")]
        public bool HoanVi { get; set; }
        [XmlElement("SoCauHoiCon")]
        public int SoCauHoiCon { get; set; }
        [XmlElement("MaCauHoiCha")]
        public string MaCauHoiCha { get; set; }
        [XmlElement("CauTraLoi")]
        public List<CauTraLoiDto> CauTraLoi { get; set; }
    }

    public class CauTraLoiDto
    {
        [XmlElement("MaCauTraLoi")]
        public string MaCauTraLoi { get; set; }
        [XmlElement("MaCauHoi")]
        public string MaCauHoi { get; set; }
        [XmlElement("NoiDung")]
        public string NoiDung { get; set; }
        [XmlElement("ThuTu")]
        public int ThuTu { get; set; }
        [XmlElement("LaDapAn")]
        public bool LaDapAn { get; set; }
        [XmlElement("HoanVi")]
        public bool HoanVi { get; set; }
    }
}
