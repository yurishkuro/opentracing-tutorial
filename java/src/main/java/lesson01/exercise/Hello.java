package lesson01.exercise;

public class Hello {

    private void sayHello(String helloTo) {
        String helloStr = String.format("Hello, %s!", helloTo);
        System.out.println(helloStr);
    }

    public static void main(String[] args) {
        if (args.length != 1) {
            throw new IllegalArgumentException("Expecting one argument");
        }
        String helloTo = args[0];
        new Hello().sayHello(helloTo);
    }
}
