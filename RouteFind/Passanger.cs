using Microsoft.AspNetCore.Components.Server.Circuits;

namespace YolBul;

public abstract class Passanger
{
    string name;
    double discount;

    public void ScanCard(){ }

}

public class Message
{
    public string Log;
    public double price;
    
    public Message(string log, double price)
    {
        this.Log = log;
        this.price = price;
    }


}

public class Student : Passanger
{
    public static string name = "Ogrenci";
    public static double discount = 0.75;
    public static Message scanCard(double price, string payment) 
    {
        if (payment == CityCard.payment)
        {
            Message message = new Message($"İşlem başarılı, {Student.name} indirimi ve {CityCard.payment} indirimi kullanildi\n({price:0.00} * {discount:0.00} * {CityCard.discount:0.00} = {CityCard.getPayment(price) * discount:0.00} tl)", (CityCard.getPayment(price) * discount));
            return message;
        }
        else if (payment == CreditCard.payment)
        {
            Message message = new Message($"İşlem başarılı. ({price}:0.00 tl)", (CreditCard.getPayment(price)));
            return message;
        }   
        else
        {
            Message message = new Message($"İşlem başarısız. ", 0);
            return message;
        }
    }
}

public class Elderly : Passanger
{
    public static string name = "Yasli";
    public static double discount = 0;

    
    public static Message scanCard(double price, string payment) 
    {
        if (payment == CityCard.payment)
        {
            Message message = new Message($"İşlem başarılı, {name} indirimi kullanildi\n({price:0.00} * {discount:0.00} = {CityCard.getPayment(price) * discount:0.00} tl)", (CityCard.getPayment(price) * discount));
            return message;
        }
        else if (payment == CreditCard.payment)
        {
            Message message = new Message($"İşlem başarılı. ({price}:0.00 tl)", (CreditCard.getPayment(price)));
            return message;
        }   
        else
        {
            Message message = new Message($"İşlem başarısız. ", 0);
            return message;
        }
    }

}

public class Standart : Passanger
{
    public static string name = "Standart";

    public static Message scanCard(double price, string payment) 
    {
        if (payment == CityCard.payment)
        {
            Message message = new Message($"İşlem başarılı, {CityCard.payment} indirimi kullanildi\n({price}:0.00 * {CityCard.discount}:0.00 = {CityCard.getPayment(price)}:0.00 tl)", (CityCard.getPayment(price)));
            return message;
        }
        else if (payment == CreditCard.payment)
        {
            Message message = new Message($"İşlem başarılı. ({price}:0.00 tl)", (CreditCard.getPayment(price)));
            return message;
        }   
        else
        {
            Message message = new Message($"İşlem başarısız. ", 0);
            return message;
        }
    }
}