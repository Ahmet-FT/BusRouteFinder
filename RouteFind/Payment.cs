namespace YolBul;


public abstract class Payment
{
    public static string payment;

    public static double getPayment(double price)
    {
        return price;
    }


}

public class CityCard : Payment
{
    public static string payment = "Kent Kart";
    public static double discount = 0.9;

    public static double getPayment(double price)
    {   
        return price * discount;
    }
}

public class CreditCard : Payment
{
    public static string payment = "kredi Kart";

    public static double getPayment(double price)
    {   
        return price;
    }

}
