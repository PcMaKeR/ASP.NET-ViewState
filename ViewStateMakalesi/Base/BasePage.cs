namespace ViewStateMakalesi.Base
{
    using System;
    using System.Collections.Generic;
    using System.IO; // MemoryStream İçin.
    using System.IO.Compression; // sıkıştırma için.
    using System.Linq;
    using System.Web;
    using System.Web.UI;

    /// <summary>
    /// Ana Sınıfımız, Bütün sınıflar buradan türeyecek.
    /// İngilizce meraklıları için Base Page
    /// </summary>
    public class BasePage : System.Web.UI.Page
    {
        /// <summary>
        /// ViewState base64 e çevrilmeden önce veriler ObjectState olarak tutulur. Bunun için ObjectStateFormatter sınıfı gerekli bize.
        /// </summary>
        private ObjectStateFormatter objFormatla = new ObjectStateFormatter();

        /// <summary>
        /// Sıkıştırma işlemini yapan metod.
        /// </summary>
        /// <param name="sikistirilacakData">Sıkıştırılacak byte dizisi</param>
        /// <returns></returns>
        private static byte[] VeriSikistirma(byte[] sikistirilacakData)
        {
            //memoryStream açalım ve using bloğu içinde kullanalım. İşi bitince direkt GC temizlesin bunu.
            //asp.net te bu tür işlemlere dikkat edin yoksa hafıza yetersiz kalır ve performans ciddi anlamda düşüşe geçer.
            using (var memoryStream = new MemoryStream())
            {
                //memorystream e sıkıştırılacak verimizi yazdırıyoruz.
                using (var gzipSikistirma = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    //parametreler:
                    //sıkıştırılacak data dizisi, 0 dan başla, sıkıştırılacak datanın boyutu
                    gzipSikistirma.Write(sikistirilacakData, 0, sikistirilacakData.Length);

                    //tekrar byte dizisi olarak geri dönelim.
                    return memoryStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Sıkıştırılmış byte dizisini çözen metodumuz.
        /// </summary>
        /// <param name="sikistirilmisData">Sıkıştırılmış Byte Dizisi</param>
        /// <returns></returns>
        private static byte[] VeriCozme(byte[] sikistirilmisData)
        {
            using (var msSikistirilmisData = new MemoryStream())
            {
                //memorystream içine byte dizimizi yazıyoruz.
                msSikistirilmisData.Write(sikistirilmisData, 0, sikistirilmisData.Length);
                //okuma konumunu 0. satıra getiriyoruz.
                msSikistirilmisData.Position = 0;

                //sıkıştırılmış veriyi çözelim.
                using (var gzipCozme = new GZipStream(msSikistirilmisData, CompressionMode.Decompress, true))
                {
                    //çözülmüş veri için memoryStream açalım.
                    using (var msCozulmusData = new MemoryStream())
                    {
                        //64 dizilik şekilde okuyalım.
                        var buffer = new byte[64];
                        //buffer okunuyor.
                        var read = gzipCozme.Read(buffer, 0, buffer.Length);

                        //buffer sıfırdan büyük olduğu sürecek oku ve bunu msCozulmusData memorystream'a yaz.
                        while (read > 0)
                        {
                            msCozulmusData.Write(buffer, 0, read);
                            read = gzipCozme.Read(buffer, 0, buffer.Length);
                        }

                        //çözülmüş veriyi geri dön.
                        return msCozulmusData.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// ViewState sayfaya kaydediliyor. Bu metodu ezmek(override) etmek zorundayız. Kendi kodumuz ile değiştiriyoruz.
        /// </summary>
        /// <param name="state">ViewState nesnesi.</param>
        protected override void SavePageStateToPersistenceMedium(object state)
        {
            using (var msViewStateByteDizisi = new MemoryStream())
            {
                //viewstate'i byte dizisine çevirmek için serialize ediyoruz.
                this.objFormatla.Serialize(msViewStateByteDizisi, state);
                
                //bytedizisine çeviriyoruz.
                var vsByteDizisi = msViewStateByteDizisi.ToArray();

                //byte dizisini çevirip sayfaya hidden value olarak saklıyoruz.
                ClientScript.RegisterHiddenField(
                    "__SIKISTIRILMIS_VS",
                    Convert.ToBase64String(VeriSikistirma(vsByteDizisi))
                );
            }
        }

        /// <summary>
        /// Postback olduğunda viewstate durumunu geri alıyoruz.
        /// </summary>
        /// <returns></returns>
        protected override object LoadPageStateFromPersistenceMedium()
        {
            //form postback olduğunda verimizi okuyoruz.
            var sikistirilmisBase64Veri = Request.Form["__SIKISTIRILMIS_VS"];
            
            //verileri açıyoruz.
            var acilmisByteDizisi = VeriCozme(Convert.FromBase64String(sikistirilmisBase64Veri));
            
            //durum bilgisini geri dönüyoruz.
            return this.objFormatla.Deserialize(Convert.ToBase64String(acilmisByteDizisi));
        }

        /// <summary>
        /// Sayfa bellekten atılıyor!
        /// </summary>
        public override void Dispose()
        {
            //objFormatla bellekte kalmasın.
            GC.SuppressFinalize(this.objFormatla);

            base.Dispose();
        }
    }
}