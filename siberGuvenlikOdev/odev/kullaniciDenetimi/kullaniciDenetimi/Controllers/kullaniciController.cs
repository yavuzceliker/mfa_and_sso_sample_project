using kullaniciDenetimi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace kullaniciDenetimi.Controllers
{
    public class kullaniciController : Controller
    {
        /*
         Yazan: Yavuz ÇELİKER
         E-Mail: yavuz@yavuzceliker.com.tr
         Tarih: 20.05.2020
         Amaç: Hazırlanacak ödev için kullanılacak kullanıcı ve oturum denetim işlemlerinin sağlanacağı proje.

         */

        modelContext mc = new modelContext();
        Random rnd = new Random();
        
        // Üç faklı şekilde de veri gelebileceği için 3 farklı route kullandım.
        [Route("")]
        [Route("kullanici/login")]
        [Route("kullanici/login/{geriDon}")]
        public ActionResult login(string geriDon)
        {
            //Görev veya muhasebe sisteminden logine yönlendirilmiş ise geriDon parametresi gorev veya muhasebe değerlerinden birini alır.
            // Bu alınan değeri ViewBag ile view'a gönderiyorum ilgili sisteme geri yönlendirme yapabilmek için.
            ViewBag.sistem = geriDon;
            return View();
        }   
        public JsonResult girisKontrol(string kullaniciAdi, string sifre, string dogrulamaKodu, string durum)
        {
            /*
             Bu fonskiyon login sayfasından veri geldiği zaman iki aşamada çalışacak.
             durum=="birinci" olan aşamada kullanıcı adı ve şifre girildiği zaman doğru veya yanlış olduğunun kontrolü için
             durum=="ikinci" olan aşamada ise e-mail adresine gönderilen doğrulama kodunun doğruluğunu kontrol etmek için.
             */
            if (durum == "birinci")
            {
                if (kullaniciAdi != null && sifre != null)
                {
                    //Kullanıcının kontrolünü sağlıyorum. Girilen kullanıcı adı ve şifrenin doğruluğunu test ediyorum. kullaniciAdi e-mail de olabilir. İki girişe de izin veriliyor. Büyük küçük harfe duyarsız.
                    kullanici kullanici = mc.kullanici.FirstOrDefault(x => (x.kullaniciAdi.ToLower() == kullaniciAdi.ToLower() || x.email.ToLower() == kullaniciAdi.ToLower()) && x.sifre == sifre);
                    if (kullanici != null)
                    {
                        if (kullanici.hesapDurumu)//Eğer ki kayıtlarla eşleşen bir kullanıcı bulur ise bu durumda hesap durumunu kontrol ediyorum. Kayıt işleminden sonra gelen mail doğrulama linkini tıklamış mı diye.
                        {
                            /*
                             Eğer ki koşul sağlanır ise bir rastgele kod üretiliyor. Bu kod bir session değişkene kullaniciId ile birlikte atılarak ikinci aşamada kullanılmak üzere kaydediliyor.
                             Bu kod daha sonra kullanıcının kayıtlı e-mail adresine gönderiliyor.
                             */
                            string kod = rnd.Next(100000, 999999).ToString();
                            Session["login"] = kod + kullanici.kullaniciId;
                            if ("OK" == MailGonder(kullanici.email, "Siber Güvenliğe Giriş Proje Ödevi Çift Faktörlü Doğrulama", "Doğrulama kodunuz:" + kod))
                                return Json("OK", JsonRequestBehavior.AllowGet);
                            else
                                return Json("hata", JsonRequestBehavior.AllowGet);
                        }
                        else
                            return Json("hesapdogrulanmamis", JsonRequestBehavior.AllowGet);

                    }
                    else
                        return Json("hata", JsonRequestBehavior.AllowGet);
                }
            }
            if (durum == "ikinci")
            {
                //Kullanıcı adı ve şifre doğrulanıp e-maile gelen doğrulama kodu alınıp sisteme girildikten sonra bu kısım çalışacak.
                if (Session["login"] != null)//Birinci adımda tanımladığımız session değişken aktifse içeriye giriyoruz.
                {

                    string gelen = Session["login"].ToString();//Session değişkenin değerini bir stringe alıyoruz sonraki aşamada parçalamak için.
                    if (gelen.Length >= 7)//Birinci aşamada 7 karakterlik bir veri gönderildiği için, ikinci aşamada bu veri sayısı kontrol ediliyor.
                    {
                        string kod = gelen.Substring(0, 6);//Gelen kod parçalanarak ilk 6 hanesi olan şifre birliştiriliyor.
                        string kullaniciId = gelen.Substring(6);// Son hane olan kullaniciId ise bir değişkene atanıyor.
                        if (kod == dogrulamaKodu)//Kullanıcının girdiği kod ile session değişkenden gelen kod uyuşuyorsa doğru yoldayız.
                        {
                            kullanici kullanici = mc.kullanici.FirstOrDefault(x => x.kullaniciId.ToString() == kullaniciId);// İlgili id ye sahip kullanıcı aranıyor.
                            if (kullanici != null)//Kullanıcı var ise
                            {
                                /*
                                 ipLogin modeli üzerinde oturum açan personellerin kaydı tutulmaktadır.
                                 */
                                ipLogin login = new ipLogin();
                                login.Kullanici = kullanici;
                                login.zaman = DateTime.Now;
                                login.ipAdresi = ipAl();// oturum açan kullanıcının ip adresini alıyoruz.
                                login.dogrulama = rastgeleKod();// Bu kod ile oturum verisini kıyaslamak için kayıt altına alıyoruz. 
                                mc.ipLogin.Add(login);
                                mc.SaveChanges();
                                //yonlendir isimli fonksiyonda kullanmak adına 3 adet TempData verisini fonksiyona iletiyorum.
                                TempData["gelen"] = gelen;
                                TempData["zaman"] = login.zaman;
                                TempData["rastgeleKod"] = login.dogrulama;
                                return Json(new string[] { "giris" }, JsonRequestBehavior.AllowGet);
                            }
                            //Aksi takdirde ilgili hata kodunu iletiyorum.

                            else
                                return Json(new string[] { "kadisifrehatali" }, JsonRequestBehavior.AllowGet);
                        }
                        else
                            return Json(new string[] { "kodhatali" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                        return Json("hata", JsonRequestBehavior.AllowGet);
                }
            }

            return Json("hata", JsonRequestBehavior.AllowGet);
        }

        public ActionResult yonlendir(string geriDon)
        {
            if (TempData["gelen"] != null && TempData["rastgeleKod"]!=null && TempData["zaman"]!=null)
            {
                string gelen = TempData["gelen"].ToString();
                var oCookie = FormsAuthentication.GetAuthCookie(gelen, false);
                var ticket = FormsAuthentication.Decrypt(oCookie.Value);

                FormsAuthenticationTicket oTicket = new FormsAuthenticationTicket(ticket.Version, gelen, (DateTime)TempData["zaman"], ((DateTime)TempData["zaman"]).AddMinutes(60), true, TempData["rastgeleKod"].ToString());
                string cookieStr = FormsAuthentication.Encrypt(oTicket);

                oCookie.Value = cookieStr;
                Response.Cookies.Add(oCookie);

                switch (geriDon)
                {
                    case "gorev":
                        {
                            Response.Redirect("https://gorev.yavuzceliker.com.tr");
                            break;
                        }
                    case "muhasebe":
                        {
                            Response.Redirect("https://muhasebe.yavuzceliker.com.tr");
                            break;
                        }
                    default:
                        {
                            Response.Redirect("https://gorev.yavuzceliker.com.tr");
                            break;
                        }
                }
            }
            return RedirectToAction("login", "kullanici");
        }

        public ActionResult sifreYenile(string kullaniciAdi)
        {
            if (kullaniciAdi != null)
            {
                kullanici kullanici = mc.kullanici.FirstOrDefault(x => (x.email.ToLower() == kullaniciAdi.ToLower() || x.kullaniciAdi.ToLower() == kullaniciAdi.ToLower()) && x.hesapDurumu);
                if (kullanici != null)
                {
                    string sifre = rastgeleKod();
                    if ("OK" == MailGonder(kullanici.email, "Siber Güvenliğe Giriş Proje Ödevi Şifre Yenileme Talebi", "Şifrenizi yenilemek için aşağıdaki linki kullanabilirsiniz.\nhttps://odev.yavuzceliker.com.tr/kullanici/sifredegistir/" + kullanici.kullaniciId + "/" + sifre))
                    {
                        TempData["msg"] = "alert('Şifrenizi değiştirebileceğiniz bağlantı kayıtlı email adresinize gönderildi.');";
                        kullanici.dogrulamaKodu = sifre;
                        mc.SaveChanges();
                        return RedirectToAction("login", "kullanici");
                    }
                }
                else
                    ViewBag.mesaj = "alert('Aktif hesap bulunamadı.');";

            }
            return View();
        }

        [Route("kullanici/sifreDegistir/{kullaniciId}/{dogrulamaKodu}")]
        public ActionResult sifreDegistir(int kullaniciId, string sifre, string dogrulamaKodu, string kaydet)
        {
            if (kaydet == "Şifremi Değiştir")
            {
                if (Session["sifreDegistir"] != null)
                {
                    string kod = Session["sifreDegistir"].ToString();
                    kullanici kullanici = mc.kullanici.FirstOrDefault(x => x.kullaniciId == kullaniciId && x.dogrulamaKodu == kod);
                    if (kullanici != null)
                    {
                        kullanici.sifre = sifre;
                        mc.SaveChanges();
                        TempData["msg"] = "alert('Şifreniz başarıyla değiştirildi.');";
                        return RedirectToAction("login", "kullanici");
                    }
                }
                else
                {
                    TempData["msg"] = "alert('İstediğiniz işleme ulaşılamıyor.');";
                    return RedirectToAction("login", "kullanici");
                }
            }
            else
            {
                kullanici kullanici = mc.kullanici.FirstOrDefault(x => x.kullaniciId == kullaniciId && x.dogrulamaKodu == dogrulamaKodu);
                if (kullanici != null)
                {
                    ViewBag.guvenlikSorusu = kullanici.soru;
                    ViewBag.kod = kullanici.dogrulamaKodu;
                    ViewBag.kullaniciId = kullanici.kullaniciId;
                }
                else
                {
                    TempData["msg"] = "alert('İstediğiniz işleme ulaşılamıyor.');";
                    return RedirectToAction("login", "kullanici");
                }

            }
            return View();
        }
        public JsonResult guvenlikSorusuDogrula(string guvenlikSorusuCevabi, string kod, int kullaniciId)
        {
            kullanici kullanici = mc.kullanici.FirstOrDefault(x => x.kullaniciId == kullaniciId && x.dogrulamaKodu == kod && x.cevap.ToLower() == guvenlikSorusuCevabi.ToLower());
            if (kullanici != null)
            {
                kullanici.dogrulamaKodu = rnd.Next(100000, 999999).ToString();
                mc.SaveChanges();
                Session["sifreDegistir"] = kullanici.dogrulamaKodu;
                return Json("OK");
            }
            else
                return Json("hata");
        }


        public ActionResult kaydol(kullanici kullanici, string kaydol)
        {
            if (kaydol == "Kaydı Tamamla")
            {
                if (kullanici.adSoyad != null ? kullanici.adSoyad != "" ? false : true : true)
                {
                    ViewBag.mesaj = "alert('Ad soyad boş olamaz.');";
                    return View();
                }
                if (kullanici.cevap != null ? kullanici.cevap != "" ? false : true : true)
                {
                    ViewBag.mesaj = "alert('Güvenlik sorusunun cevabı boş olamaz.');";
                    return View();
                }
                if (kullanici.email != null ? kullanici.email != "" ? false : true : true)
                {
                    ViewBag.mesaj = "alert('E-mail boş olamaz.');";
                    return View();
                }
                else
                {
                    if (mc.kullanici.FirstOrDefault(x => x.email.ToLower() == kullanici.email.ToLower()) != null)
                    {
                        ViewBag.mesaj = "alert('E-mail kullanımda.');";
                        return View();
                    }
                }

                if (kullanici.kullaniciAdi != null ? kullanici.kullaniciAdi != "" ? false : true : true)
                {
                    ViewBag.mesaj = "alert('Kullanıcı adı boş olamaz.');";
                    return View();
                }
                else
                {
                    if (mc.kullanici.FirstOrDefault(x => x.kullaniciAdi.ToLower() == kullanici.kullaniciAdi.ToLower()) != null)
                    {
                        ViewBag.mesaj = "alert('Kullanıcı adı kullanımda.');";
                        return View();
                    }
                }
                if (kullanici.sifre != null ? kullanici.sifre != "" ? false : true : true)
                {
                    ViewBag.mesaj = "alert('Şifre boş olamaz.');";
                    return View();
                }
                if (kullanici.soru != null ? kullanici.soru != "" ? false : true : true)
                {
                    ViewBag.mesaj = "alert('Güvenlik sorusu boş olamaz.');";
                    return View();
                }
                kullanici.dogrulamaKodu = rnd.Next(100000, 999999).ToString();
                mc.kullanici.Add(kullanici);
                mc.SaveChanges();
                if ("OK" == MailGonder(kullanici.email, "Siber Güvenliğe Giriş Proje Ödevi Hesap Doğrulama İşleminiz", "Hesabınızı doğrulamak için lütfen tıklayınız.\n\nhttps://odev.yavuzceliker.com.tr/kullanici/dogrula/" + kullanici.kullaniciId + "/" + kullanici.dogrulamaKodu))
                    TempData["msg"] = "alert('Kaydınız başarıyla alındı. E-mail adresinizi kontrol ediniz.');";
                return RedirectToAction("login", "kullanici");

            }
            return View();
        }
        [Route("kullanici/dogrula/{kullaniciId}/{dogrulamaKodu}")]
        public ActionResult dogrula(int kullaniciId, string dogrulamaKodu)
        {
            kullanici kullanici = mc.kullanici.FirstOrDefault(x => x.kullaniciId == kullaniciId && !x.hesapDurumu && x.dogrulamaKodu == dogrulamaKodu);
            if (kullanici != null)
            {
                kullanici.hesapDurumu = true;
                mc.SaveChanges();
                TempData["msg"] = "alert('Hesabınız başarıyla onaylanmıştır.');";
            }
            else
                TempData["msg"] = "alert('İstediğiniz işleme ulaşılamıyor.');";

            return RedirectToAction("login", "kullanici");
        }
        public JsonResult girisBilgileriKontrol(string email, string kullaniciAdi, string adSoyad)
        {
            if (adSoyad == "")
            {
                ViewBag.mesaj = "alert('Ad soyad boş olamaz.');";
                return Json("asb");
            }

            if (kullaniciAdi == "")
            {
                ViewBag.mesaj = "alert('Kullanıcı adı boş olamaz.');";
                return Json("kab");
            }
            if (mc.kullanici.FirstOrDefault(x => x.kullaniciAdi.ToLower() == kullaniciAdi.ToLower()) != null)
            {
                ViewBag.mesaj = "alert('Kullanıcı adı kullanımda.');";
                return Json("ka");
            }

            if (email == "")
            {
                ViewBag.mesaj = "alert('E-mail boş olamaz.');";
                return Json("emb");
            }
            if (mc.kullanici.FirstOrDefault(x => x.email.ToLower() == email.ToLower()) != null)
            {
                ViewBag.mesaj = "alert('E-mail kullanımda.');";
                return Json("em");
            }
            return Json("OK");
        }


        public string MailGonder(string EMail, string Konu, string Mesaj)
        {
            string durum = "";
            try
            {
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                NetworkCredential credentials = new NetworkCredential();
                credentials.UserName = "siberguvenlikproje@gmail.com";
                credentials.Password = "siber123";
                smtp.Credentials = credentials;

                MailAddress addressFrom = new MailAddress(credentials.UserName);
                MailAddress addressTo = new MailAddress(EMail);
                MailMessage msg = new MailMessage(addressFrom, addressTo);
                msg.Subject = Konu;
                msg.Body = Mesaj;

                smtp.Send(msg);
                return durum = "OK";
            }
            catch (Exception hata)
            {
                return durum = hata.Message.ToString();
            }

        }

        public string ipAl()
        {
            var ipAddress = string.Empty;
            if (HttpContext.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
                ipAddress = HttpContext.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();
            else if (HttpContext.Request.ServerVariables["HTTP_CLIENT_IP"] != null && HttpContext.Request.ServerVariables["HTTP_CLIENT_IP"].Length != 0)
                ipAddress = HttpContext.Request.ServerVariables["HTTP_CLIENT_IP"];
            else if (HttpContext.Request.UserHostAddress.Length != 0)
                ipAddress = HttpContext.Request.UserHostName;
            return ipAddress;
        }
        public string rastgeleKod()
        {
            string karakterler = "1qwerty2uıopğü3asdfgh4jklşiz5xcvbnm6öçQWER7TYUIOP8ĞÜASDF9GHJKLŞ0İZXCVBNMÖÇ";
            string kod = "";
            for (int i = 0; i < 6; i++)
                kod += karakterler[rnd.Next(0, karakterler.Length)].ToString();
            return kod;
        }
    }
}