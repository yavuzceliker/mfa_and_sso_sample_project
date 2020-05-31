using muhasebe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace muhasebe.Controllers
{
    public class homeController : Controller
    {
        /*
             Yazan: Yavuz ÇELİKER
             E-Mail: yavuz@yavuzceliker.com.tr
             Tarih: 20.05.2020
             Amaç: Oluşturulan 3 projeli sistem için kullanılacak olan örnek muhasebe sistemi.
             
             */
        modelContext mc = new modelContext();
        kullanici kullanici = null;

        public bool login()
        {
            /*
             Yazan: Yavuz ÇELİKER
             E-Mail: yavuz@yavuzceliker.com.tr
             Tarih: 20.05.2020
             Amaç: Sistem üzerindeki oturum kontrolü yapılacak tüm sayfalar için bu fonksiyon çağrılmalıdır.
             Cookie üzerinden gelen veriyi kontrol ederek oturum açılıp açılmadığı denetlenecek.
             Fonksiyon true döndürdüğü zaman oturum açılmış demektir. Eğer ki false döndürürse bir hata oluşmuştur veya henüz oturum açılmamıştır.
             
             */
            if (Request.Cookies[".siberGuvenlikAuth"] != null)//Oturum kontrol cookie'sini .siberGuvenlikAuth olarak isimlendirdim. Boş mu değil mi diye bakıyorum.
            {
                if (!string.IsNullOrEmpty(Request.Cookies[".siberGuvenlikAuth"].Value))
                {
                    HttpCookie cookie = Request.Cookies[".siberGuvenlikAuth"];//Eğer ki boş değilse ve içerisinde bir değer varsa bu değişkeni bir cookie içerisine atıyorum.

                    FormsAuthenticationTicket bilet = FormsAuthentication.Decrypt(cookie.Value); // Cookie değerini çözümleyerek bir bilet haline getiriyorum.
                    FormsIdentity kimlik = new FormsIdentity(bilet); // Bilet içerisindeki kimlik bilgilerini okuyorum.

                    if (kimlik.IsAuthenticated) //Kimlik doğrulanmışsa içeriye girecek.
                    {
                        if (kimlik.Name.Length >= 7)//Kimlik.name değeri içerisinde 7 karakterlik bir veri döndürdüm. Doğru olması için 7 karakter olmalı minimum.
                        {
                            string
                                kullaniciId = kimlik.Name.Substring(6), // 7. karakteri kullaniciId olarak gönderdim ve gönderdiğim değeri karşılıyorum.
                                gizliKod = kimlik.Ticket.UserData; // Kullanıcıya ait oluşturulan gizli kod ise userdata içerisinde gönderdim ve karşıladım.

                            //Cookie ile gelen kullaniciId ve o kullanıcıya bağladığım gizli kodu karşılaştırarak mevcut oturum ile devam eden bir kullanıcı var mı kontrol ediyorum.
                            ipLogin login = mc.ipLogin.FirstOrDefault(x => x.Kullanici.kullaniciId.ToString() == kullaniciId && x.dogrulama == gizliKod);
                            if (login != null)
                            {
                                //kullanıcının oturum süresi ile cookie tarafından gelen doğrulamanın oluşturulma zamanı birebir aynı olduğundan zamanları karşılaştırıyorum.
                                if (login.zaman.ToString("dd.MM.yyyy HH:mm:ss") == kimlik.Ticket.IssueDate.ToString("dd.MM.yyyy HH:mm:ss"))
                                {
                                    //Bir sakınca olmazsa eğer kullanıcıyı belirlemiş oluyoruz ve dilediğimiz yerde kullanabiliyoruz.
                                    kullanici = login.Kullanici;
                                    return true;
                                }
                                //Diğer tüm koşullar sağlamadığı için false döndüdüyoruz.
                                else
                                    return false;
                            }
                            else
                                return false;
                        }
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
                return false;

        }
        public ActionResult Index()
        {

            if (!login())
                ViewBag.oturumKontrol = false;
            else
            {
                ViewBag.kullanici = kullanici.adSoyad;
                ViewBag.oturumKontrol = true;
            }
            return View();
        }
        public ActionResult logout()
        {
            //Mecvut oturumu sonlandırarak login sayfasına geri yönlendiriyorum.
            FormsAuthentication.SignOut();
            return Redirect("https://odev.yavuzceliker.com.tr/kullanici/login/muhasebe");
        }
    }
}