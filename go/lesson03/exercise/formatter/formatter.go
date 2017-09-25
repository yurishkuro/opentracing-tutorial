package main

import (
	"fmt"
	"log"
	"net/http"
)

func main() {
	http.HandleFunc("/format", func(w http.ResponseWriter, r *http.Request) {
		helloTo := r.FormValue("helloTo")
		helloStr := fmt.Sprintf("Hello, %s!", helloTo)
		w.Write([]byte(helloStr))
	})

	log.Fatal(http.ListenAndServe(":8081", nil))
}
