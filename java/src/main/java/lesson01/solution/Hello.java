package lesson01.solution;

public class Hello {

    public static void main(String[] args) {
        if (args.length != 1) {
            throw new IllegalArgumentException("Expecting one argument");
        }
        String helloTo = args[0];
        String helloStr = String.format("Hello, %s!", helloTo);
        System.out.println(helloStr);
    }

}
